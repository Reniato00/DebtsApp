using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace application.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly IServiceProvider _services;

    public ApiClient(HttpClient http, TokenStore tokens, IServiceProvider services)
    {
        _http = http;
        _tokens = tokens;
        _services = services;
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendAsync<T>(req);
    }

    public async Task<T?> PostAsync<T>(string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Content = JsonContent.Create(body, null, Json);
        req.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        return await SendAsync<T>(req);
    }

    public async Task<T?> PutAsync<T>(string path, object body)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, path);
        req.Content = JsonContent.Create(body, null, Json);
        return await SendAsync<T>(req);
    }

    public async Task<byte[]> GetByteArrayAsync(string path)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyToken(req);
        var res = await _http.SendAsync(req);

        var needRetry = res.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_tokens.RefreshToken);
        if (needRetry)
        {
            req.Dispose();
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                req = new HttpRequestMessage(HttpMethod.Get, path);
                ApplyToken(req);
                res = await _http.SendAsync(req);
            }
        }

        res.EnsureSuccessStatusCode();
        var bytes = await res.Content.ReadAsByteArrayAsync();
        req.Dispose();
        return bytes;
    }

    public async Task DeleteAsync(string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, path);
        await SendAsync<object?>(req);
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage req)
    {
        ApplyToken(req);
        var res = await _http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(_tokens.RefreshToken))
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                req = await CloneRequestAsync(req);
                ApplyToken(req);
                res = await _http.SendAsync(req);
            }
        }

        res.EnsureSuccessStatusCode();

        if (typeof(T) == typeof(object))
            return default;

        return await res.Content.ReadFromJsonAsync<T>(Json);
    }

    private async Task<bool> TryRefreshAsync()
    {
        var refreshToken = _tokens.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh");
            req.Content = JsonContent.Create(new { refreshToken }, null, Json);
            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                await _tokens.ClearAsync();
                return false;
            }

            var result = await res.Content.ReadFromJsonAsync<LoginResponse>(Json);
            if (result == null)
            {
                await _tokens.ClearAsync();
                return false;
            }

            var userId = DecodeUserId(result.AccessToken);
            await _tokens.SaveAsync(userId, result.AccessToken, result.RefreshToken);

            // Notify auth state provider that we refreshed
            var authState = _services.GetService<AuthenticationStateProvider>() as Auth.CustomAuthStateProvider;
            authState?.NotifyAuthStateChanged();

            return true;
        }
        catch
        {
            await _tokens.ClearAsync();
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        if (req.Content != null)
        {
            var contentBytes = await req.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            if (req.Content.Headers.ContentType != null)
                clone.Content.Headers.ContentType = req.Content.Headers.ContentType;
        }

        foreach (var header in req.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    private void ApplyToken(HttpRequestMessage req)
    {
        var token = _tokens.AccessToken;
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (_tokens.UserId != Guid.Empty)
            req.Headers.Add("UserId", _tokens.UserId.ToString());
    }

    private static Guid DecodeUserId(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return Guid.Empty;
            var payload = parts[1];
            var len = payload.Length % 4;
            if (len == 2) payload += "==";
            else if (len == 3) payload += "=";
            payload = payload.Replace('-', '+').Replace('_', '/');
            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);
            using var doc = JsonDocument.Parse(json);
            foreach (var key in new[] { "nameid", "sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" })
                if (doc.RootElement.TryGetProperty(key, out var prop) && Guid.TryParse(prop.GetString(), out var uid))
                    return uid;
        }
        catch { }
        return Guid.Empty;
    }
}

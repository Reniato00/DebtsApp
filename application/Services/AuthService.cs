using System.Text;
using System.Text.Json;

namespace application.Services;

public class AuthService
{
    private readonly ApiClient _api;
    private readonly TokenStore _tokenStore;

    public AuthService(ApiClient api, TokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var result = await _api.PostAsync<LoginResponse>("api/auth/login", new { email, password });
        if (result is null)
            throw new Exception("Credenciales inválidas");

        var userId = DecodeUserId(result.AccessToken);
        await _tokenStore.SaveAsync(userId, result.AccessToken, result.RefreshToken);
        return result;
    }

    public async Task<LoginResponse> RegisterAsync(string email, string name, string password)
    {
        var result = await _api.PostAsync<LoginResponse>("api/auth/register", new { email, name, password });
        if (result is null)
            throw new Exception("No se pudo registrar");

        var userId = DecodeUserId(result.AccessToken);
        await _tokenStore.SaveAsync(userId, result.AccessToken, result.RefreshToken);
        return result;
    }

    public async Task<LoginResponse?> TryRefreshAsync()
    {
        var refresh = _tokenStore.RefreshToken;
        if (string.IsNullOrEmpty(refresh))
            return null;

        try
        {
            var result = await _api.PostAsync<LoginResponse>("api/auth/refresh", new { refreshToken = refresh });
            if (result is null) return null;

            var userId = DecodeUserId(result.AccessToken);
            await _tokenStore.SaveAsync(userId, result.AccessToken, result.RefreshToken);
            return result;
        }
        catch
        {
            await _tokenStore.ClearAsync();
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try { await _api.PostAsync<object>("api/auth/logout", new { }); }
        catch { }
        await _tokenStore.ClearAsync();
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

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}

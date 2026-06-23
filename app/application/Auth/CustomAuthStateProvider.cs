using System.Security.Claims;
using application.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace application.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenStore _tokenStore;

    public CustomAuthStateProvider(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _tokenStore.AccessToken;

        if (string.IsNullOrEmpty(token))
            return Task.FromResult(NotAuthenticated());

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _tokenStore.UserId.ToString()),
            new Claim(ClaimTypes.Name, "User")
        }, "jwt");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public Task NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    private static AuthenticationState NotAuthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}

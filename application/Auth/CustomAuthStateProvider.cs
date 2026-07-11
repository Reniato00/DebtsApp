using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace application.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private Guid _userId;
    private string _userName = "";

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_userId == Guid.Empty)
            return Task.FromResult(NotAuthenticated());

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Name, _userName)
        }, "session");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void SignIn(Guid userId, string userName)
    {
        _userId = userId;
        _userName = userName;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        _userId = Guid.Empty;
        _userName = "";
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState NotAuthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}

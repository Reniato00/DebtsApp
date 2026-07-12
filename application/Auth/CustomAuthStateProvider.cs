using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace application.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private Guid _userId;
    private string _userName = "";
    private DateTime _lastActivity = DateTime.MinValue;

    private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(20);

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_userId == Guid.Empty)
            return Task.FromResult(NotAuthenticated());

        if (DateTime.UtcNow - _lastActivity > SessionTimeout)
        {
            SignOut();
            return Task.FromResult(NotAuthenticated());
        }

        _lastActivity = DateTime.UtcNow;

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
        _lastActivity = DateTime.UtcNow;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        _userId = Guid.Empty;
        _userName = "";
        _lastActivity = DateTime.MinValue;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState NotAuthenticated() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}

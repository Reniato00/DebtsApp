using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace application.Helpers;

public static class AuthHelper
{
    public static async Task<Guid?> GetUserIdOrRedirect(AuthenticationStateProvider authState, NavigationManager nav)
    {
        var auth = await authState.GetAuthenticationStateAsync();
        if (!auth.User.Identity?.IsAuthenticated ?? true)
        {
            nav.NavigateTo("/login");
            return null;
        }

        var uid = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(uid, out var userId))
        {
            nav.NavigateTo("/login");
            return null;
        }

        return userId;
    }
}

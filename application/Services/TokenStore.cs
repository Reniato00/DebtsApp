namespace application.Services;

public class TokenStore
{
    public string AccessToken { get; private set; } = "";
    public string RefreshToken { get; private set; } = "";
    public Guid UserId { get; private set; }

    public Task SaveAsync(Guid userId, string accessToken, string refreshToken)
    {
        UserId = userId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        UserId = Guid.Empty;
        AccessToken = "";
        RefreshToken = "";
        return Task.CompletedTask;
    }
}

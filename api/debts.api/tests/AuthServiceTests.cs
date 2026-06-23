using application.Services;
using domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using persistence.Repositories;

namespace tests;

[TestClass]
public class AuthServiceTests
{
    private readonly Mock<IRefreshTokenRepository> refreshRepoMock = new();
    private readonly Mock<IUserRepository> userRepoMock = new();
    private static readonly string PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");

    private AuthService CreateService(Dictionary<string, string?>? extraConfig = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "ThisIsASecretKeyThatIsAtLeast32CharactersLong!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };
        if (extraConfig != null)
            foreach (var kv in extraConfig)
                dict[kv.Key] = kv.Value;

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        return new AuthService(config, refreshRepoMock.Object, userRepoMock.Object);
    }

    [TestMethod]
    public async Task RegisterAsync_Valid_ReturnsTokens()
    {
        var service = CreateService();
        userRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        userRepoMock.Setup(r => r.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) =>
            {
                u.PasswordHash = PasswordHash;
                return u;
            });

        var (accessToken, refreshToken, expiresIn) =
            await service.RegisterAsync("new@test.com", "New User", "123456");

        Assert.IsFalse(string.IsNullOrEmpty(accessToken));
        Assert.IsFalse(string.IsNullOrEmpty(refreshToken));
        Assert.IsTrue(expiresIn > 0);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        userRepoMock.Setup(r => r.GetByEmailAsync("existing@test.com"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "existing@test.com" });
        await CreateService().RegisterAsync("existing@test.com", "Existing", "123456");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task RegisterAsync_InvalidPassword_Throws() =>
        await CreateService().RegisterAsync("a@b.com", "Name", "12345");

    [TestMethod]
    public async Task LoginAsync_Valid_ReturnsTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@test.com", PasswordHash = PasswordHash };
        userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        var service = CreateService();

        var (accessToken, refreshToken, expiresIn) =
            await service.LoginAsync("test@test.com", "123456");

        Assert.IsFalse(string.IsNullOrEmpty(accessToken));
        Assert.IsFalse(string.IsNullOrEmpty(refreshToken));
        Assert.IsTrue(expiresIn > 0);
    }

    [TestMethod]
    [ExpectedException(typeof(UnauthorizedAccessException))]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Test", Email = "test@test.com", PasswordHash = PasswordHash };
        userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        await CreateService().LoginAsync("test@test.com", "wrong-password");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task LoginAsync_EmptyEmail_Throws() =>
        await CreateService().LoginAsync("", "123456");

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task LoginAsync_EmptyPassword_Throws() =>
        await CreateService().LoginAsync("test@test.com", "");

    [TestMethod]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Test User", Email = "test@test.com", PasswordHash = PasswordHash };
        userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        RefreshToken? stored = null;
        refreshRepoMock.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(rt => stored = rt)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var (_, refreshToken, _) = await service.LoginAsync("test@test.com", "123456");

        refreshRepoMock.Setup(r => r.GetByTokenHashAsync(stored!.TokenHash))
            .ReturnsAsync(stored);

        var (newAccess, newRefresh, _) = await service.RefreshTokenAsync(refreshToken);

        Assert.IsFalse(string.IsNullOrEmpty(newAccess));
        Assert.IsFalse(string.IsNullOrEmpty(newRefresh));
        Assert.AreNotEqual(refreshToken, newRefresh);
    }

    [TestMethod]
    [ExpectedException(typeof(UnauthorizedAccessException))]
    public async Task RefreshTokenAsync_InvalidToken_Throws() =>
        await CreateService().RefreshTokenAsync("invalid-refresh-token");

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task RefreshTokenAsync_EmptyToken_Throws() =>
        await CreateService().RefreshTokenAsync("");

    [TestMethod]
    public async Task LogoutAsync_Valid_RevokesAll()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        await service.LogoutAsync(userId);

        refreshRepoMock.Verify(r => r.RevokeAllForUserAsync(userId), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task LogoutAsync_EmptyUserId_Throws() =>
        await CreateService().LogoutAsync(Guid.Empty);
}

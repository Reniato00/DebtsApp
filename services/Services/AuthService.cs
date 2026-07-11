using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using persistence.Repositories;
using domain.Entities;

namespace services.Services
{
    public interface IAuthService
    {
        Task<(string accessToken, string refreshToken, int expiresIn)> RegisterAsync(string email, string name, string password);
        Task<(string accessToken, string refreshToken, int expiresIn)> LoginAsync(string email, string password);
        Task<(string accessToken, string refreshToken, int expiresIn)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration configuration;
        private readonly IRefreshTokenRepository refreshTokenRepo;
        private readonly IUserRepository userRepo;

        public AuthService(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepo, IUserRepository userRepo)
        {
            this.configuration = configuration;
            this.refreshTokenRepo = refreshTokenRepo;
            this.userRepo = userRepo;
        }

        public async Task<(string accessToken, string refreshToken, int expiresIn)> RegisterAsync(string email, string name, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters");

            var existing = await userRepo.GetByEmailAsync(email);
            if (existing != null)
                throw new InvalidOperationException("A user with this email already exists");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            var created = await userRepo.CreateUserAsync(user);
            if (created == null)
                throw new Exception("Failed to create user");

            return await GenerateTokenPairAsync(created.Id, created.Name);
        }

        public async Task<(string accessToken, string refreshToken, int expiresIn)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required");

            var user = await userRepo.GetByEmailAsync(email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password");

            return await GenerateTokenPairAsync(user.Id, user.Name);
        }

        public async Task<(string accessToken, string refreshToken, int expiresIn)> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token is required");

            var tokenHash = HashToken(refreshToken);
            var stored = await refreshTokenRepo.GetByTokenHashAsync(tokenHash);

            if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            await refreshTokenRepo.RevokeAsync(stored.Id);

            var user = await userRepo.GetByIdAsync(stored.UserId);
            var name = user?.Name ?? "Unknown";

            return await GenerateTokenPairAsync(stored.UserId, name);
        }

        public async Task LogoutAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            await refreshTokenRepo.RevokeAllForUserAsync(userId);
        }

        private string GenerateAccessToken(Guid userId, string name)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var secret = jwtSettings["Secret"]!;
            var issuer = jwtSettings["Issuer"]!;
            var audience = jwtSettings["Audience"]!;
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"]!);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<(string accessToken, string refreshToken, int expiresIn)> GenerateTokenPairAsync(Guid userId, string name)
        {
            var accessToken = GenerateAccessToken(userId, name);
            var (refreshToken, refreshTokenHash) = GenerateRefreshToken();

            var expiresIn = int.Parse(configuration.GetSection("Jwt")["ExpirationMinutes"]!);
            var refreshExpirationDays = int.Parse(
                configuration.GetSection("Jwt")["RefreshTokenExpirationDays"] ?? "7");

            await refreshTokenRepo.CreateAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpirationDays),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            });

            return (accessToken, refreshToken, expiresIn * 60);
        }

        private static (string raw, string hash) GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var raw = Convert.ToBase64String(randomBytes);
            return (raw, HashToken(raw));
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}

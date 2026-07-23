using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using persistence.Repositories;
using domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using services.Logging;

namespace services.Services
{
    public interface IAuthService
    {
        Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password, string termsVersion = "1.0", string? turnstileToken = null);
        Task<(Guid userId, string name)> LoginAsync(string email, string password);
        Task LogoutAsync(Guid userId);
        Task DeleteAccountAsync(Guid userId);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepo;
        private readonly ITermsAcceptanceRepository termsRepo;
        private readonly IDebtRepository debtRepo;
        private readonly IPaymentRepository paymentRepo;
        private readonly IRefreshTokenRepository refreshTokenRepo;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthService> logger;
        private static readonly ConcurrentDictionary<string, LoginAttempt> _failedAttempts = new();
        private static readonly ConcurrentDictionary<string, DateTime> _registerLog = new();
        private static readonly ConcurrentDictionary<string, int> _ipRegisterCount = new();
        private static readonly ConcurrentDictionary<string, DateTime> _ipRegisterWindow = new();
        private static readonly ConcurrentDictionary<string, DateTime> _deletedAccounts = new();
        private static readonly ConcurrentDictionary<string, int> _changePasswordCount = new();
        private static readonly ConcurrentDictionary<string, DateTime> _changePasswordWindow = new();

        private const int MaxLoginAttempts = 5;
        private const int MaxRegisterAttempts = 3;
        private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RegisterWindow = TimeSpan.FromHours(1);
        private static readonly TimeSpan IpRegisterWindow = TimeSpan.FromHours(1);
        private const int MaxRegistrationsPerIp = 3;
        private static readonly TimeSpan DeletedAccountCooldown = TimeSpan.FromHours(24);
        private static readonly TimeSpan ChangePasswordWindow = TimeSpan.FromHours(1);
        private const int MaxChangePasswordAttempts = 3;
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public AuthService(IUserRepository userRepo, ITermsAcceptanceRepository termsRepo,
            IDebtRepository debtRepo, IPaymentRepository paymentRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            this.userRepo = userRepo;
            this.termsRepo = termsRepo;
            this.debtRepo = debtRepo;
            this.paymentRepo = paymentRepo;
            this.refreshTokenRepo = refreshTokenRepo;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password, string termsVersion = "1.0", string? turnstileToken = null)
        {
            var ip = GetClientIp();
            AppLogger.Auth(logger, "Register attempt", email: email, ip: ip, success: false);

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (!EmailRegex.IsMatch(email))
                throw new ArgumentException("El correo no tiene un formato válido");

            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
                throw new ArgumentException("El nombre debe tener al menos 2 caracteres");

            var normalizedEmail = email.Trim().ToLowerInvariant();
            CheckDeletedAccountCooldown(normalizedEmail);
            CheckRegisterRateLimit(normalizedEmail);
            CheckIpRegisterRateLimit();
            await VerifyTurnstileToken(turnstileToken);
            ValidatePassword(password);

            _registerLog.AddOrUpdate(normalizedEmail, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);

            var existing = await userRepo.GetByEmailAsync(normalizedEmail);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con este correo");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            var created = await userRepo.CreateUserAsync(user);
            if (created == null)
                throw new Exception("Error al crear el usuario");

            await termsRepo.CreateAsync(new TermsAcceptance
            {
                Id = Guid.NewGuid(),
                UserId = created.Id,
                Version = termsVersion,
                AcceptedAt = DateTime.UtcNow
            });

            _registerLog.TryRemove(normalizedEmail, out _);
            AppLogger.Auth(logger, "Register", userId: created.Id, email: normalizedEmail, ip: ip, success: true);
            return (created.Id, created.Name);
        }

        public async Task<(Guid userId, string name)> LoginAsync(string email, string password)
        {
            var ip = GetClientIp();
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es requerida");

            CheckLoginRateLimit(email);

            var user = await userRepo.GetByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                RecordFailedAttempt(email);
                AppLogger.Auth(logger, "Login", email: email, ip: ip, success: false, details: "Invalid credentials");
                throw new UnauthorizedAccessException("Correo o contraseña incorrectos");
            }

            _failedAttempts.TryRemove(email, out _);
            AppLogger.Auth(logger, "Login", userId: user.Id, email: email, ip: ip, success: true);
            return (user.Id, user.Name);
        }

        public Task LogoutAsync(Guid userId)
        {
            AppLogger.Auth(logger, "Logout", userId: userId);
            return Task.CompletedTask;
        }

        public async Task DeleteAccountAsync(Guid userId)
        {
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado");

            AppLogger.AccountDeletion(logger, userId, user.Email, GetClientIp());

            var debts = await debtRepo.GetAllAsync(userId);
            var debtIds = debts.Select(d => d.Id).ToList();

            if (debtIds.Any())
                await paymentRepo.DeleteByDebtIdsAsync(debtIds);

            await debtRepo.DeleteAllByUserIdAsync(userId);
            await termsRepo.DeleteByUserIdAsync(userId);
            await refreshTokenRepo.DeleteByUserIdAsync(userId);
            await userRepo.DeleteByIdAsync(userId);

            _deletedAccounts[user.Email] = DateTime.UtcNow;
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var ip = GetClientIp();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado");

            try
            {
                CheckChangePasswordRateLimit(userId.ToString());
            }
            catch (InvalidOperationException ex)
            {
                AppLogger.PasswordChange(logger, userId, ip, success: false, reason: ex.Message);
                throw;
            }

            if (string.IsNullOrWhiteSpace(currentPassword))
                throw new ArgumentException("La contraseña actual es requerida");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("La nueva contraseña es requerida");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                AppLogger.PasswordChange(logger, userId, ip, success: false, reason: "Invalid current password");
                throw new UnauthorizedAccessException("La contraseña actual es incorrecta");
            }

            if (currentPassword == newPassword)
                throw new ArgumentException("La nueva contraseña debe ser diferente a la actual");

            ValidatePassword(newPassword);

            var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var updatedUser = new User { Id = userId, Name = user.Name, Email = user.Email, PasswordHash = newHash };
            await userRepo.UpdateUserAsync(updatedUser);

            await refreshTokenRepo.RevokeAllForUserAsync(userId);
            AppLogger.PasswordChange(logger, userId, ip, success: true);
        }

        private string? GetClientIp()
        {
            return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }

        private void CheckChangePasswordRateLimit(string userId)
        {
            if (_changePasswordWindow.TryGetValue(userId, out var windowStart))
            {
                if (DateTime.UtcNow - windowStart > ChangePasswordWindow)
                {
                    _changePasswordWindow.TryRemove(userId, out _);
                    _changePasswordCount.TryRemove(userId, out _);
                    return;
                }

                if (_changePasswordCount.TryGetValue(userId, out var count) && count >= MaxChangePasswordAttempts)
                {
                    var remaining = (int)(ChangePasswordWindow - (DateTime.UtcNow - windowStart)).TotalMinutes;
                    throw new InvalidOperationException(
                        $"Demasiados cambios de contraseña. Intenta de nuevo en {remaining} minuto(s).");
                }

                _changePasswordCount.AddOrUpdate(userId, 1, (_, c) => c + 1);
            }
            else
            {
                _changePasswordWindow[userId] = DateTime.UtcNow;
                _changePasswordCount[userId] = 1;
            }
        }

        private async Task VerifyTurnstileToken(string? token)
        {
            var secretKey = configuration["Turnstile:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                return;

            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Debes completar la verificación de seguridad.");

            using var client = new HttpClient();
            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "secret", secretKey },
                    { "response", token }
                }));

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>();
            if (result == null || !result.Success)
                throw new InvalidOperationException("La verificación de seguridad falló. Intenta de nuevo.");
        }

        private class TurnstileVerifyResponse
        {
            public bool Success { get; set; }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres");

            if (!password.Any(char.IsUpper))
                throw new ArgumentException("La contraseña debe contener al menos una mayúscula");

            if (!password.Any(char.IsLower))
                throw new ArgumentException("La contraseña debe contener al menos una minúscula");

            if (!password.Any(char.IsDigit))
                throw new ArgumentException("La contraseña debe contener al menos un número");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                throw new ArgumentException("La contraseña debe contener al menos un carácter especial (@, #, $, etc.)");
        }

        private void CheckDeletedAccountCooldown(string email)
        {
            if (_deletedAccounts.TryGetValue(email, out var deletedAt))
            {
                var elapsed = DateTime.UtcNow - deletedAt;
                if (elapsed < DeletedAccountCooldown)
                {
                    var remaining = (int)(DeletedAccountCooldown - elapsed).TotalHours;
                    throw new InvalidOperationException(
                        $"Esta cuenta fue eliminada recientemente. Puedes registrarla de nuevo en {remaining} hora(s).");
                }
                _deletedAccounts.TryRemove(email, out _);
            }
        }

        private void CheckIpRegisterRateLimit()
        {
            var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ip)) return;

            if (_ipRegisterWindow.TryGetValue(ip, out var windowStart))
            {
                if (DateTime.UtcNow - windowStart > IpRegisterWindow)
                {
                    _ipRegisterWindow.TryRemove(ip, out _);
                    _ipRegisterCount.TryRemove(ip, out _);
                    return;
                }

                if (_ipRegisterCount.TryGetValue(ip, out var count) && count >= MaxRegistrationsPerIp)
                {
                    var remaining = (int)(IpRegisterWindow - (DateTime.UtcNow - windowStart)).TotalMinutes;
                    throw new InvalidOperationException(
                        $"Demasiados registros desde esta dirección IP. Intenta de nuevo en {remaining} minuto(s).");
                }

                _ipRegisterCount.AddOrUpdate(ip, 1, (_, c) => c + 1);
            }
            else
            {
                _ipRegisterWindow[ip] = DateTime.UtcNow;
                _ipRegisterCount[ip] = 1;
            }
        }

        private void CheckRegisterRateLimit(string email)
        {
            if (_registerLog.TryGetValue(email, out var lastAttempt))
            {
                if (DateTime.UtcNow - lastAttempt < RegisterWindow)
                {
                    throw new InvalidOperationException(
                        "Ya solicitaste un registro recientemente. Espera una hora antes de intentar de nuevo.");
                }
            }
        }

        private void CheckLoginRateLimit(string email)
        {
            if (_failedAttempts.TryGetValue(email, out var attempt))
            {
                if (attempt.Count >= MaxLoginAttempts)
                {
                    var elapsed = DateTime.UtcNow - attempt.FirstAttempt;
                    if (elapsed < LoginLockoutDuration)
                    {
                        var remaining = (int)(LoginLockoutDuration - elapsed).TotalMinutes;
                        throw new UnauthorizedAccessException(
                            $"Demasiados intentos. Intenta de nuevo en {remaining} minuto(s).");
                    }
                    _failedAttempts.TryRemove(email, out _);
                }
            }
        }

        private void RecordFailedAttempt(string email)
        {
            var now = DateTime.UtcNow;
            _failedAttempts.AddOrUpdate(email,
                _ => new LoginAttempt { Count = 1, FirstAttempt = now },
                (_, existing) =>
                {
                    if (now - existing.FirstAttempt > LoginLockoutDuration)
                        return new LoginAttempt { Count = 1, FirstAttempt = now };
                    existing.Count++;
                    return existing;
                });
        }

        private class LoginAttempt
        {
            public int Count { get; set; }
            public DateTime FirstAttempt { get; set; }
        }
    }
}

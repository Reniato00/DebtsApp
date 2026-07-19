using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using persistence.Repositories;
using domain.Entities;

namespace services.Services
{
    public interface IAuthService
    {
        Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password, string termsVersion = "1.0");
        Task<(Guid userId, string name)> LoginAsync(string email, string password);
        Task LogoutAsync(Guid userId);
        Task DeleteAccountAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepo;
        private readonly ITermsAcceptanceRepository termsRepo;
        private readonly IDebtRepository debtRepo;
        private readonly IPaymentRepository paymentRepo;
        private readonly IRefreshTokenRepository refreshTokenRepo;
        private static readonly ConcurrentDictionary<string, LoginAttempt> _failedAttempts = new();
        private static readonly ConcurrentDictionary<string, DateTime> _registerLog = new();

        private const int MaxLoginAttempts = 5;
        private const int MaxRegisterAttempts = 3;
        private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RegisterWindow = TimeSpan.FromHours(1);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public AuthService(IUserRepository userRepo, ITermsAcceptanceRepository termsRepo,
            IDebtRepository debtRepo, IPaymentRepository paymentRepo,
            IRefreshTokenRepository refreshTokenRepo)
        {
            this.userRepo = userRepo;
            this.termsRepo = termsRepo;
            this.debtRepo = debtRepo;
            this.paymentRepo = paymentRepo;
            this.refreshTokenRepo = refreshTokenRepo;
        }

        public async Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password, string termsVersion = "1.0")
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (!EmailRegex.IsMatch(email))
                throw new ArgumentException("El correo no tiene un formato válido");

            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
                throw new ArgumentException("El nombre debe tener al menos 2 caracteres");

            CheckRegisterRateLimit(email);
            ValidatePassword(password);

            _registerLog.AddOrUpdate(email, _ => DateTime.UtcNow, (_, _) => DateTime.UtcNow);

            var existing = await userRepo.GetByEmailAsync(email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con este correo");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Email = email.Trim().ToLowerInvariant(),
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

            _registerLog.TryRemove(email, out _);
            return (created.Id, created.Name);
        }

        public async Task<(Guid userId, string name)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es requerida");

            CheckLoginRateLimit(email);

            var user = await userRepo.GetByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                RecordFailedAttempt(email);
                throw new UnauthorizedAccessException("Correo o contraseña incorrectos");
            }

            _failedAttempts.TryRemove(email, out _);
            return (user.Id, user.Name);
        }

        public Task LogoutAsync(Guid userId)
        {
            return Task.CompletedTask;
        }

        public async Task DeleteAccountAsync(Guid userId)
        {
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado");

            var debts = await debtRepo.GetAllAsync(userId);
            var debtIds = debts.Select(d => d.Id).ToList();

            if (debtIds.Any())
                await paymentRepo.DeleteByDebtIdsAsync(debtIds);

            await debtRepo.DeleteAllByUserIdAsync(userId);
            await termsRepo.DeleteByUserIdAsync(userId);
            await refreshTokenRepo.DeleteByUserIdAsync(userId);
            await userRepo.DeleteByIdAsync(userId);
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

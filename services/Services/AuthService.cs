using System.Collections.Concurrent;
using persistence.Repositories;
using domain.Entities;

namespace services.Services
{
    public interface IAuthService
    {
        Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password);
        Task<(Guid userId, string name)> LoginAsync(string email, string password);
        Task LogoutAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository userRepo;
        private static readonly ConcurrentDictionary<string, LoginAttempt> _failedAttempts = new();

        private const int MaxAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthService(IUserRepository userRepo)
        {
            this.userRepo = userRepo;
        }

        public async Task<(Guid userId, string name)> RegisterAsync(string email, string name, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre es requerido");

            ValidatePassword(password);

            var existing = await userRepo.GetByEmailAsync(email);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un usuario con este correo");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            var created = await userRepo.CreateUserAsync(user);
            if (created == null)
                throw new Exception("Error al crear el usuario");

            return (created.Id, created.Name);
        }

        public async Task<(Guid userId, string name)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es requerido");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es requerida");

            CheckRateLimit(email);

            var user = await userRepo.GetByEmailAsync(email);
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

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres");

            if (!password.Any(char.IsUpper))
                throw new ArgumentException("La contraseña debe contener al menos una mayúscula");

            if (!password.Any(char.IsDigit))
                throw new ArgumentException("La contraseña debe contener al menos un número");
        }

        private void CheckRateLimit(string email)
        {
            if (_failedAttempts.TryGetValue(email, out var attempt))
            {
                if (attempt.Count >= MaxAttempts)
                {
                    var elapsed = DateTime.UtcNow - attempt.FirstAttempt;
                    if (elapsed < LockoutDuration)
                    {
                        var remaining = (int)(LockoutDuration - elapsed).TotalMinutes;
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
                    if (now - existing.FirstAttempt > LockoutDuration)
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

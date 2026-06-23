using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using persistence.Repositories;

namespace application.Services
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(string name, string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> UpdateUserAsync(Guid id, string name, string email);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly IMemoryCache cache;
        private readonly int cacheTtlMinutes;

        public UserService(IUserRepository userRepository, IMemoryCache cache, IConfiguration configuration)
        {
            this.userRepository = userRepository;
            this.cache = cache;
            cacheTtlMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var ttl) ? ttl : 60;
        }

        private static string UserKey(Guid id) => $"user:get:{id}";

        public async Task<User> CreateUserAsync(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email
            };

            var created = await userRepository.CreateUserAsync(user);
            return created ?? throw new Exception("Failed to create user");
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = UserKey(id);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await userRepository.GetByIdAsync(id);
            });
        }

        public async Task<User?> UpdateUserAsync(Guid id, string name, string email)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required");

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            var user = new User
            {
                Id = id,
                Name = name,
                Email = email
            };

            var updated = await userRepository.UpdateUserAsync(user);

            if (updated != null)
                cache.Remove(UserKey(id));

            return updated;
        }
    }
}
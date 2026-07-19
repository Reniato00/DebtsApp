using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> CreateUserAsync(User user);
        Task<User?> UpdateUserAsync(User user);
        Task DeleteByIdAsync(Guid id);
    }

    public class UserRepository : IUserRepository
    {
        private readonly ISqlConnectionFactory factory;

        public UserRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetById, new { Id = id });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetByEmail, new { Email = email });
        }

        public async Task<User?> CreateUserAsync(User user)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(UserQueries.CreateUser, user);
            if (rowsAffected == 0) return null;
            return await GetByIdAsync(user.Id);
        }

        public async Task<User?> UpdateUserAsync(User user)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(UserQueries.UpdateUser, user);
            if (rowsAffected == 0) return null;
            return await GetByIdAsync(user.Id);
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(UserQueries.DeleteById, new { Id = id });
        }
    }
}

using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface IIdempotencyRepository
    {
        Task<IdempotencyKey?> GetByKeyAsync(string key);
        Task<bool> CreateAsync(IdempotencyKey entry);
        Task DeleteExpiredAsync();
    }

    public class IdempotencyRepository : IIdempotencyRepository
    {
        private readonly ISqlConnectionFactory factory;

        public IdempotencyRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task<IdempotencyKey?> GetByKeyAsync(string key)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<IdempotencyKey>(
                IdempotencyQueries.GetByKey, new { Key = key });
        }

        public async Task<bool> CreateAsync(IdempotencyKey entry)
        {
            using var connection = factory.CreateConnection();
            var rows = await connection.ExecuteAsync(IdempotencyQueries.Insert, entry);
            return rows > 0;
        }

        public async Task DeleteExpiredAsync()
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(IdempotencyQueries.DeleteExpired);
        }
    }
}

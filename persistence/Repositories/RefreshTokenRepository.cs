using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task CreateAsync(RefreshToken token);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task RevokeAsync(Guid id);
        Task RevokeAllForUserAsync(Guid userId);
        Task DeleteByUserIdAsync(Guid userId);
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ISqlConnectionFactory factory;

        public RefreshTokenRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task CreateAsync(RefreshToken token)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(RefreshTokenQueries.Create, token);
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                RefreshTokenQueries.GetByToken, new { TokenHash = tokenHash });
        }

        public async Task RevokeAsync(Guid id)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(RefreshTokenQueries.Revoke,
                new { Id = id, RevokedAt = DateTime.UtcNow });
        }

        public async Task RevokeAllForUserAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(RefreshTokenQueries.RevokeAllForUser,
                new { UserId = userId, RevokedAt = DateTime.UtcNow });
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(RefreshTokenQueries.DeleteByUserId, new { UserId = userId });
        }
    }
}

using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface ITermsAcceptanceRepository
    {
        Task CreateAsync(TermsAcceptance acceptance);
        Task<IEnumerable<TermsAcceptance>> GetByUserIdAsync(Guid userId);
        Task DeleteByUserIdAsync(Guid userId);
    }

    public class TermsAcceptanceRepository : ITermsAcceptanceRepository
    {
        private readonly ISqlConnectionFactory factory;

        public TermsAcceptanceRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task CreateAsync(TermsAcceptance acceptance)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(TermsAcceptanceQueries.Create, acceptance);
        }

        public async Task<IEnumerable<TermsAcceptance>> GetByUserIdAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<TermsAcceptance>(TermsAcceptanceQueries.GetByUserId, new { UserId = userId });
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(TermsAcceptanceQueries.DeleteByUserId, new { UserId = userId });
        }
    }
}

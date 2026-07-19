using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Models;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface IDebtRepository
    {
        Task<IEnumerable<Debt>> GetAllAsync(Guid userId);
        Task<(IEnumerable<Debt> Items, int TotalCount)> GetAllPagedAsync(Guid userId, int page, int pageSize);
        Task<int> CountAsync(Guid userId);
        Task<Debt?> CreateDebtAsync(Debt debt);
        Task<Debt?> GetByIdAsync(Guid id);
        Task<Debt?> UpdateDebtAsync(Debt debt);
        Task<bool> DeleteDebtAsync(Guid id, Guid userId);
        Task<bool> PayOffDebtAsync(Guid id, Guid userId);
        Task<IEnumerable<Debt>> SearchDebtsAsync(Guid userId, string query);
        Task<IEnumerable<Debt>> GetOverdueDebtsAsync(Guid userId);
        Task<decimal> GetTotalAmountDebtsAsync(Guid userId);
        Task<decimal> GetTotalAmountMonthlyPaymentAsync(Guid userId);
        Task<DebtSummaryResult> GetDebtSummaryAsync(Guid userId);
        Task<IEnumerable<Debt>> GetUpcomingPaymentsAsync(Guid userId);
        Task DeleteAllByUserIdAsync(Guid userId);
    }

    public class DebtRepository : IDebtRepository
    {
        private readonly ISqlConnectionFactory factory;

        public DebtRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task<IEnumerable<Debt>> GetAllAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Debt>(DebtQueries.GetAll, new { UserId = userId });
        }

        public async Task<(IEnumerable<Debt> Items, int TotalCount)> GetAllPagedAsync(Guid userId, int page, int pageSize)
        {
            using var connection = factory.CreateConnection();
            var offset = (page - 1) * pageSize;

            using var multi = await connection.QueryMultipleAsync(
                $"{DebtQueries.Count} {DebtQueries.GetAllPaged}",
                new { UserId = userId, Offset = offset, PageSize = pageSize });

            var totalCount = await multi.ReadFirstAsync<int>();
            var items = await multi.ReadAsync<Debt>();

            return (items, totalCount);
        }

        public async Task<int> CountAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(DebtQueries.Count, new { UserId = userId });
        }

        public async Task<Debt?> CreateDebtAsync(Debt debt)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(DebtQueries.CreateDebt, debt);

            if (rowsAffected == 0) return null;

            return await GetByIdAsync(debt.Id);
        }

        public async Task<Debt?> GetByIdAsync(Guid id)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Debt>(DebtQueries.GetByIdAfterInsert, new { Id = id });
        }

        public async Task<Debt?> UpdateDebtAsync(Debt debt)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(DebtQueries.UpdateDebt, debt);
            if (rowsAffected == 0) return null;
            return await GetByIdAsync(debt.Id);
        }

        public async Task<bool> DeleteDebtAsync(Guid id, Guid userId)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(DebtQueries.DeleteDebt, new { Id = id, UserId = userId });
            return rowsAffected > 0;
        }

        public async Task<decimal> GetTotalAmountDebtsAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(DebtQueries.TotalAmountDebts, new { UserId = userId });
        }

        public async Task<decimal> GetTotalAmountMonthlyPaymentAsync(Guid userId) 
        {
            using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(DebtQueries.TotalAmountMonthlyPayment, new { UserId = userId });
        }

        public async Task<DebtSummaryResult> GetDebtSummaryAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstAsync<DebtSummaryResult>(DebtQueries.DebtSummary, new { UserId = userId });
        }

        public async Task<bool> PayOffDebtAsync(Guid id, Guid userId)
        {
            using var connection = factory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(DebtQueries.PayOffDebt, new { Id = id, UserId = userId });
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<Debt>> SearchDebtsAsync(Guid userId, string query)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Debt>(DebtQueries.SearchDebts, new { UserId = userId, Query = $"%{query}%" });
        }

        public async Task<IEnumerable<Debt>> GetOverdueDebtsAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Debt>(DebtQueries.OverdueDebts, new { UserId = userId });
        }

        public async Task<IEnumerable<Debt>> GetUpcomingPaymentsAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Debt>(DebtQueries.UpcomingPayments, new { UserId = userId });
        }

        public async Task DeleteAllByUserIdAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(DebtQueries.DeleteByUserId, new { UserId = userId });
        }
    }
}

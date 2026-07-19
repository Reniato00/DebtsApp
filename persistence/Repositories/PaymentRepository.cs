using System.Text.Json;
using Dapper;
using domain.Entities;
using persistence.Connection;
using persistence.Models;
using persistence.Queries;

namespace persistence.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Payment>> GetByDebtIdAsync(Guid debtId);
        Task<Payment?> CreatePaymentAsync(Payment payment, string? idempotencyKey = null);
        Task<bool> DeletePaymentAsync(Guid id);
        Task<bool> UpdatePaymentAsync(Guid id, decimal newAmount);
        Task<IEnumerable<Payment>> GetPaymentHistoryByUserAsync(Guid userId);
        Task<IEnumerable<PaymentWithDebtName>> GetPaymentHistoryWithDebtNameAsync(Guid userId);
        Task<(IEnumerable<PaymentWithDebtName> Items, int TotalCount)> GetPaymentHistoryWithDebtNamePagedAsync(Guid userId, int page, int pageSize);
        Task<decimal> GetTotalByDebtAsync(Guid debtId);
        Task<IEnumerable<MonthlySpendingItem>> GetMonthlySpendingAsync(Guid userId);
        Task<decimal> GetTotalPaymentsByUserAsync(Guid userId);
        Task DeleteByDebtIdsAsync(IEnumerable<Guid> debtIds);
    }

    public class PaymentRepository : IPaymentRepository
    {
        private readonly ISqlConnectionFactory factory;

        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PaymentRepository(ISqlConnectionFactory factory)
        {
            this.factory = factory;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Payment>(PaymentQueries.GetById, new { Id = id });
        }

        public async Task<IEnumerable<Payment>> GetByDebtIdAsync(Guid debtId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Payment>(PaymentQueries.GetByDebtId, new { DebtId = debtId });
        }

        public async Task<Payment?> CreatePaymentAsync(Payment payment, string? idempotencyKey = null)
        {
            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var existing = await GetIdempotencyKeyAsync(idempotencyKey);
                if (existing != null && existing.StatusCode == 201)
                {
                    try { return JsonSerializer.Deserialize<Payment>(existing.ResponseJson, Json); }
                    catch { }
                }
            }

            using var connection = factory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var rowsAffected = await connection.ExecuteAsync(PaymentQueries.CreatePayment, payment, transaction);
            if (rowsAffected == 0)
            {
                transaction.Rollback();
                return null;
            }

            await connection.ExecuteAsync(PaymentQueries.ReduceDebtBalance,
                new { payment.DebtId, payment.Amount }, transaction);

            var created = await connection.QueryFirstOrDefaultAsync<Payment>(
                PaymentQueries.GetById, new { payment.Id }, transaction);

            if (created != null && !string.IsNullOrEmpty(idempotencyKey))
            {
                var json = JsonSerializer.Serialize(created, Json);
                await connection.ExecuteAsync(IdempotencyQueries.Insert, new
                {
                    Key = idempotencyKey,
                    ResponseJson = json,
                    StatusCode = 201,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                }, transaction);
            }

            transaction.Commit();

            return created;
        }

        private async Task<IdempotencyKey?> GetIdempotencyKeyAsync(string key)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<IdempotencyKey>(
                IdempotencyQueries.GetByKey, new { Key = key });
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            var payment = await GetByIdAsync(id);
            if (payment == null) return false;

            using var connection = factory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(PaymentQueries.DeletePayment, new { Id = id }, transaction);
            await connection.ExecuteAsync(PaymentQueries.RevertDebtBalance,
                new { payment.DebtId, payment.Amount }, transaction);

            transaction.Commit();
            return true;
        }

        public async Task<bool> UpdatePaymentAsync(Guid id, decimal newAmount)
        {
            var payment = await GetByIdAsync(id);
            if (payment == null) return false;

            var difference = newAmount - payment.Amount;

            using var connection = factory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(PaymentQueries.UpdatePayment,
                new { Id = id, Amount = newAmount }, transaction);

            if (difference > 0)
            {
                await connection.ExecuteAsync(PaymentQueries.ReduceDebtBalance,
                    new { payment.DebtId, Amount = difference }, transaction);
            }
            else if (difference < 0)
            {
                await connection.ExecuteAsync(PaymentQueries.RevertDebtBalance,
                    new { payment.DebtId, Amount = Math.Abs(difference) }, transaction);
            }

            transaction.Commit();
            return true;
        }

        public async Task<IEnumerable<Payment>> GetPaymentHistoryByUserAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<Payment>(PaymentQueries.GetPaymentHistoryByUser, new { UserId = userId });
        }

        public async Task<IEnumerable<PaymentWithDebtName>> GetPaymentHistoryWithDebtNameAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<PaymentWithDebtName>(PaymentQueries.GetPaymentHistoryWithDebtName, new { UserId = userId });
        }

        public async Task<(IEnumerable<PaymentWithDebtName> Items, int TotalCount)> GetPaymentHistoryWithDebtNamePagedAsync(Guid userId, int page, int pageSize)
        {
            using var connection = factory.CreateConnection();
            var offset = (page - 1) * pageSize;

            using var multi = await connection.QueryMultipleAsync(
                $"{PaymentQueries.CountPaymentsByUser} {PaymentQueries.GetPaymentHistoryWithDebtNamePaged}",
                new { UserId = userId, Offset = offset, PageSize = pageSize });

            var totalCount = await multi.ReadFirstAsync<int>();
            var items = await multi.ReadAsync<PaymentWithDebtName>();

            return (items, totalCount);
        }

        public async Task<decimal> GetTotalByDebtAsync(Guid debtId)
        {
            using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(PaymentQueries.GetTotalByDebt, new { DebtId = debtId });
        }

        public async Task<IEnumerable<MonthlySpendingItem>> GetMonthlySpendingAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.QueryAsync<MonthlySpendingItem>(PaymentQueries.GetMonthlySpending, new { UserId = userId });
        }

        public async Task<decimal> GetTotalPaymentsByUserAsync(Guid userId)
        {
            using var connection = factory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(PaymentQueries.GetTotalPaymentsByUser, new { UserId = userId });
        }

        public async Task DeleteByDebtIdsAsync(IEnumerable<Guid> debtIds)
        {
            using var connection = factory.CreateConnection();
            await connection.ExecuteAsync(PaymentQueries.DeleteByDebtIds, new { DebtIds = debtIds });
        }
    }
}

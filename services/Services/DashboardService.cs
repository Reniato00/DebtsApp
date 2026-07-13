using services.Models;
using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using persistence.Models;
using persistence.Repositories;

namespace services.Services
{
    public interface IDashboardService
    {
        Task<int> GetDebtCountAsync(Guid userId);
        Task<decimal> GetTotalAmountDebtsAsync(Guid userId);
        Task<decimal> GetTotalAmountMonthlyPaymentAsync(Guid userId);
        Task<DebtSummaryResult> GetDebtSummaryAsync(Guid userId);
        Task<IEnumerable<Debt>> GetUpcomingPaymentsAsync(Guid userId);
        Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId);
        Task<IEnumerable<MonthlySpendingItem>> GetMonthlySpendingAsync(Guid userId);
        Task<IEnumerable<DebtProjectionItem>> GetDebtProjectionAsync(Guid userId);
        Task<InterestCostResult> GetInterestCostAsync(Guid userId);
        Task<PaidVsPendingResult> GetPaidVsPendingAsync(Guid userId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IDebtRepository debtRepository;
        private readonly IPaymentRepository paymentRepository;
        private readonly IMemoryCache cache;
        private readonly int cacheTtlMinutes;

        public DashboardService(IDebtRepository debtRepository, IPaymentRepository paymentRepository, IMemoryCache cache, IConfiguration configuration)
        {
            this.debtRepository = debtRepository;
            this.paymentRepository = paymentRepository;
            this.cache = cache;
            cacheTtlMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var ttl) ? ttl : 60;
        }

        private static string Prefix(Guid userId) => $"dashboard:{userId}:";

        public async Task<int> GetDebtCountAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}debtcount";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.CountAsync(userId);
            });
        }

        public async Task<decimal> GetTotalAmountDebtsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}totalamount";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.GetTotalAmountDebtsAsync(userId);
            });
        }

        public async Task<decimal> GetTotalAmountMonthlyPaymentAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}totalmonthly";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.GetTotalAmountMonthlyPaymentAsync(userId);
            });
        }

        public async Task<DebtSummaryResult> GetDebtSummaryAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}summary";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.GetDebtSummaryAsync(userId);
            });
        }

        public async Task<IEnumerable<Debt>> GetUpcomingPaymentsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await debtRepository.GetUpcomingPaymentsAsync(userId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await paymentRepository.GetPaymentHistoryByUserAsync(userId);
        }

        public async Task<IEnumerable<MonthlySpendingItem>> GetMonthlySpendingAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await paymentRepository.GetMonthlySpendingAsync(userId);
        }

        public async Task<IEnumerable<DebtProjectionItem>> GetDebtProjectionAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var debts = await debtRepository.GetAllAsync(userId);

            return debts.Select(d =>
            {
                var months = d.MonthlyPayment > 0
                    ? (int)Math.Ceiling(d.CurrentBalance / d.MonthlyPayment)
                    : 0;

                return new DebtProjectionItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    CurrentBalance = d.CurrentBalance,
                    MonthlyPayment = d.MonthlyPayment,
                    MonthsToPayOff = months,
                    EstimatedPayOffDate = months > 0
                        ? DateTime.UtcNow.AddMonths(months)
                        : null
                };
            });
        }

        public async Task<InterestCostResult> GetInterestCostAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}interestcost";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                var debts = await debtRepository.GetAllAsync(userId);

                return new InterestCostResult
                {
                    TotalOriginalAmount = debts.Sum(d => d.OriginalAmount),
                    TotalCurrentBalance = debts.Sum(d => d.CurrentBalance),
                    AverageInterestRate = debts.Any() ? debts.Average(d => d.InterestRate) : 0
                };
            });
        }

        public async Task<PaidVsPendingResult> GetPaidVsPendingAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = $"{Prefix(userId)}paidvspending";
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                var totalPending = await debtRepository.GetTotalAmountDebtsAsync(userId);
                var totalPaid = await paymentRepository.GetTotalPaymentsByUserAsync(userId);
                var total = totalPaid + totalPending;

                return new PaidVsPendingResult
                {
                    TotalPaid = totalPaid,
                    TotalPending = totalPending,
                    PaidPercentage = total > 0 ? Math.Round(totalPaid / total * 100, 2) : 0
                };
            });
        }
    }
}

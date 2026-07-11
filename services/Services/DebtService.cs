using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using persistence.Repositories;

namespace services.Services
{
    public interface IDebtService
    {
        Task<Debt> CreateDebtAsync(Debt debt);
        Task<Debt?> GetByIdAsync(Guid id, Guid userId);
        Task<IEnumerable<Debt>> GetAllDebtsAsync(Guid userId);
        Task<(IEnumerable<Debt> Items, int TotalCount)> GetAllDebtsPagedAsync(Guid userId, int page, int pageSize);
        Task<Debt?> UpdateDebtAsync(Debt debt);
        Task<bool> DeleteDebtAsync(Guid id, Guid userId);
        Task<bool> PayOffDebtAsync(Guid id, Guid userId);
        Task<IEnumerable<Debt>> SearchDebtsAsync(Guid userId, string query);
        Task<IEnumerable<Debt>> GetOverdueDebtsAsync(Guid userId);
    }

    public class DebtService : IDebtService
    {
        private readonly IDebtRepository debtRepository;
        private readonly IMemoryCache cache;
        private readonly int cacheTtlMinutes;

        public DebtService(IDebtRepository debtRepository, IMemoryCache cache, IConfiguration configuration)
        {
            this.debtRepository = debtRepository;
            this.cache = cache;
            cacheTtlMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var ttl) ? ttl : 60;
        }

        private static string DebtKey(Guid userId, Guid id) => $"debt:get:{userId}:{id}";
        private static string DebtAllKey(Guid userId) => $"debt:all:{userId}";
        private static string DashboardPrefix(Guid userId) => $"dashboard:{userId}:";

        private void InvalidateUserCache(Guid userId)
        {
            cache.Remove(DebtAllKey(userId));
            var dashboardKeys = new[]
            {
                $"{DashboardPrefix(userId)}debtcount",
                $"{DashboardPrefix(userId)}totalamount",
                $"{DashboardPrefix(userId)}totalmonthly",
                $"{DashboardPrefix(userId)}summary",
                $"{DashboardPrefix(userId)}interestcost",
                $"{DashboardPrefix(userId)}paidvspending"
            };
            foreach (var key in dashboardKeys)
                cache.Remove(key);
        }

        public async Task<Debt> CreateDebtAsync(Debt debt)
        {
            if (debt.UserId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (string.IsNullOrWhiteSpace(debt.Name))
                throw new ArgumentException("Debt name is required");

            if (debt.OriginalAmount <= 0)
                throw new ArgumentException("OriginalAmount must be greater than zero");

            if (debt.CurrentBalance < 0)
                throw new ArgumentException("CurrentBalance cannot be negative");

            if (debt.InterestRate < 0 || debt.InterestRate > 100)
                throw new ArgumentException("InterestRate must be between 0 and 100");

            if (debt.MonthlyPayment <= 0)
                throw new ArgumentException("MonthlyPayment must be greater than zero");

            if (debt.StartDate >= debt.DueDate)
                throw new ArgumentException("StartDate must be before DueDate");

            debt.Id = Guid.NewGuid();

            var created = await debtRepository.CreateDebtAsync(debt);

            if (created != null)
                InvalidateUserCache(debt.UserId);

            return created ?? throw new Exception("Failed to create debt");
        }

        public async Task<Debt?> GetByIdAsync(Guid id, Guid userId)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Debt Id is required");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = DebtKey(userId, id);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.GetByIdAsync(id);
            });
        }

        public async Task<IEnumerable<Debt>> GetAllDebtsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = DebtAllKey(userId);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await debtRepository.GetAllAsync(userId);
            });
        }

        public async Task<(IEnumerable<Debt> Items, int TotalCount)> GetAllDebtsPagedAsync(Guid userId, int page, int pageSize)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await debtRepository.GetAllPagedAsync(userId, page, pageSize);
        }

        public async Task<Debt?> UpdateDebtAsync(Debt debt)
        {
            if (debt.Id == Guid.Empty)
                throw new ArgumentException("Debt Id is required");

            if (debt.UserId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (string.IsNullOrWhiteSpace(debt.Name))
                throw new ArgumentException("Debt name is required");

            if (debt.OriginalAmount <= 0)
                throw new ArgumentException("OriginalAmount must be greater than zero");

            if (debt.CurrentBalance < 0)
                throw new ArgumentException("CurrentBalance cannot be negative");

            if (debt.InterestRate < 0 || debt.InterestRate > 100)
                throw new ArgumentException("InterestRate must be between 0 and 100");

            if (debt.MonthlyPayment <= 0)
                throw new ArgumentException("MonthlyPayment must be greater than zero");

            if (debt.StartDate >= debt.DueDate)
                throw new ArgumentException("StartDate must be before DueDate");

            var updated = await debtRepository.UpdateDebtAsync(debt);

            if (updated != null)
            {
                cache.Remove(DebtKey(debt.UserId, debt.Id));
                InvalidateUserCache(debt.UserId);
            }

            return updated;
        }

        public async Task<bool> DeleteDebtAsync(Guid id, Guid userId)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Debt Id is required");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var deleted = await debtRepository.DeleteDebtAsync(id, userId);

            if (deleted)
            {
                cache.Remove(DebtKey(userId, id));
                InvalidateUserCache(userId);
            }

            return deleted;
        }

        public async Task<bool> PayOffDebtAsync(Guid id, Guid userId)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Debt Id is required");

            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var paid = await debtRepository.PayOffDebtAsync(id, userId);

            if (paid)
            {
                cache.Remove(DebtKey(userId, id));
                InvalidateUserCache(userId);
            }

            return paid;
        }

        public async Task<IEnumerable<Debt>> SearchDebtsAsync(Guid userId, string query)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query is required");

            return await debtRepository.SearchDebtsAsync(userId, query);
        }

        public async Task<IEnumerable<Debt>> GetOverdueDebtsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await debtRepository.GetOverdueDebtsAsync(userId);
        }
    }
}

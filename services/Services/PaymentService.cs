using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using persistence.Models;
using persistence.Repositories;

namespace services.Services
{
    public interface IPaymentService
    {
        Task<Payment> CreatePaymentAsync(Guid debtId, decimal amount, string paymentType = "regular", bool wasOnTime = true, string? prepaymentEffect = null, string? idempotencyKey = null);
        Task<IEnumerable<Payment>> GetByDebtIdAsync(Guid debtId);
        Task<Payment?> GetByIdAsync(Guid id);
        Task<bool> DeletePaymentAsync(Guid id);
        Task<bool> UpdatePaymentAsync(Guid id, decimal newAmount);
        Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId);
        Task<IEnumerable<PaymentWithDebtName>> GetPaymentHistoryWithDebtNameAsync(Guid userId);
        Task<(IEnumerable<PaymentWithDebtName> Items, int TotalCount)> GetPaymentHistoryWithDebtNamePagedAsync(Guid userId, int page, int pageSize);
        Task<decimal> GetTotalByDebtAsync(Guid debtId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository paymentRepository;
        private readonly IDebtRepository debtRepository;
        private readonly IMemoryCache cache;
        private readonly int cacheTtlMinutes;

        public PaymentService(IPaymentRepository paymentRepository, IDebtRepository debtRepository, IMemoryCache cache, IConfiguration configuration)
        {
            this.paymentRepository = paymentRepository;
            this.debtRepository = debtRepository;
            this.cache = cache;
            cacheTtlMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var ttl) ? ttl : 60;
        }

        private static string PaymentKey(Guid id) => $"payment:get:{id}";
        private static string PaymentByDebtKey(Guid debtId) => $"payment:debt:{debtId}";
        private static string PaymentHistoryKey(Guid userId) => $"payment:history:{userId}";
        private static string PaymentTotalKey(Guid debtId) => $"payment:total:{debtId}";
        private static string DashboardPrefix(Guid userId) => $"dashboard:{userId}:";

        private void InvalidatePaymentCache(Guid? debtId, Guid userId)
        {
            if (debtId.HasValue)
            {
                cache.Remove(PaymentByDebtKey(debtId.Value));
                cache.Remove(PaymentTotalKey(debtId.Value));
                cache.Remove($"debt:get:{userId}:{debtId}");
            }
            cache.Remove(PaymentHistoryKey(userId));
            cache.Remove($"debt:all:{userId}");

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

        private async Task<Guid> GetUserIdByDebtIdAsync(Guid debtId)
        {
            var debt = await debtRepository.GetByIdAsync(debtId);
            return debt?.UserId ?? Guid.Empty;
        }

        public async Task<Payment> CreatePaymentAsync(Guid debtId, decimal amount, string paymentType = "regular", bool wasOnTime = true, string? prepaymentEffect = null, string? idempotencyKey = null)
        {
            if (debtId == Guid.Empty)
                throw new ArgumentException("DebtId is required");

            if (amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero");

            var debt = await debtRepository.GetByIdAsync(debtId);
            if (debt == null)
                throw new KeyNotFoundException("Debt not found");

            decimal? originalMonthlyPayment = null;

            if (paymentType == "prepayment" && prepaymentEffect == "reducePayment")
            {
                originalMonthlyPayment = debt.MonthlyPayment;

                var monthlyRate = debt.InterestRate / 100m / 12m;
                var newBalance = Math.Max(0, debt.CurrentBalance - amount);

                if (newBalance > 0 && monthlyRate > 0 && debt.DueDate > DateTime.UtcNow)
                {
                    var monthsRemaining = (debt.DueDate.Year - DateTime.UtcNow.Year) * 12 +
                                          (debt.DueDate.Month - DateTime.UtcNow.Month);
                    if (monthsRemaining > 0)
                    {
                        var r = (double)monthlyRate;
                        var n = (double)monthsRemaining;
                        var factor = r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1);
                        var newPayment = (decimal)factor * newBalance;
                        debt.MonthlyPayment = Math.Round(newPayment, 2);
                        await debtRepository.UpdateDebtAsync(debt);
                    }
                }
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                DebtId = debtId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentType = paymentType,
                WasOnTime = wasOnTime,
                PrepaymentEffect = paymentType == "prepayment" ? prepaymentEffect : null,
                OriginalMonthlyPayment = originalMonthlyPayment
            };

            var created = await paymentRepository.CreatePaymentAsync(payment, idempotencyKey);

            if (created != null)
            {
                var userId = debt.UserId;
                InvalidatePaymentCache(debtId, userId);
            }

            return created ?? throw new Exception("Failed to create payment");
        }

        public async Task<IEnumerable<Payment>> GetByDebtIdAsync(Guid debtId)
        {
            if (debtId == Guid.Empty)
                throw new ArgumentException("DebtId is required");

            var key = PaymentByDebtKey(debtId);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await paymentRepository.GetByDebtIdAsync(debtId);
            });
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Payment Id is required");

            var key = PaymentKey(id);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await paymentRepository.GetByIdAsync(id);
            });
        }

        public async Task<bool> DeletePaymentAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Payment Id is required");

            var payment = await paymentRepository.GetByIdAsync(id);
            if (payment == null) return false;

            var userId = await GetUserIdByDebtIdAsync(payment.DebtId);
            var deleted = await paymentRepository.DeletePaymentAsync(id);

            if (deleted)
            {
                cache.Remove(PaymentKey(id));
                InvalidatePaymentCache(payment.DebtId, userId);
            }

            return deleted;
        }

        public async Task<bool> UpdatePaymentAsync(Guid id, decimal newAmount)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Payment Id is required");

            if (newAmount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero");

            var payment = await paymentRepository.GetByIdAsync(id);
            if (payment == null) return false;

            var userId = await GetUserIdByDebtIdAsync(payment.DebtId);
            var updated = await paymentRepository.UpdatePaymentAsync(id, newAmount);

            if (updated)
            {
                cache.Remove(PaymentKey(id));
                InvalidatePaymentCache(payment.DebtId, userId);
            }

            return updated;
        }

        public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await paymentRepository.GetPaymentHistoryByUserAsync(userId);
        }

        public async Task<IEnumerable<PaymentWithDebtName>> GetPaymentHistoryWithDebtNameAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var key = PaymentHistoryKey(userId);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await paymentRepository.GetPaymentHistoryWithDebtNameAsync(userId);
            });
        }

        public async Task<(IEnumerable<PaymentWithDebtName> Items, int TotalCount)> GetPaymentHistoryWithDebtNamePagedAsync(Guid userId, int page, int pageSize)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            return await paymentRepository.GetPaymentHistoryWithDebtNamePagedAsync(userId, page, pageSize);
        }

        public async Task<decimal> GetTotalByDebtAsync(Guid debtId)
        {
            if (debtId == Guid.Empty)
                throw new ArgumentException("DebtId is required");

            var key = PaymentTotalKey(debtId);
            return await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTtlMinutes);
                return await paymentRepository.GetTotalByDebtAsync(debtId);
            });
        }
    }
}

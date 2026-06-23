using application.Models;
using persistence.Repositories;

namespace application.Services
{
    public interface ICalculatorService
    {
        Task<PayoffStrategyResult> GetPayoffStrategyAsync(Guid userId);
        Task<DailyInterestResult> GetDailyInterestAsync(Guid userId);
    }

    public class CalculatorService : ICalculatorService
    {
        private readonly IDebtRepository debtRepository;

        public CalculatorService(IDebtRepository debtRepository)
        {
            this.debtRepository = debtRepository;
        }

        public async Task<PayoffStrategyResult> GetPayoffStrategyAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var debts = (await debtRepository.GetAllAsync(userId))
                .Where(d => d.CurrentBalance > 0)
                .ToList();

            var totalMonthly = debts.Sum(d => d.MonthlyPayment);

            var snowball = debts
                .OrderBy(d => d.CurrentBalance)
                .Select((d, i) => ToPayoffStep(d, i + 1))
                .ToList();

            var avalanche = debts
                .OrderByDescending(d => d.InterestRate)
                .ThenBy(d => d.CurrentBalance)
                .Select((d, i) => ToPayoffStep(d, i + 1))
                .ToList();

            return new PayoffStrategyResult
            {
                Snowball = snowball,
                Avalanche = avalanche,
                TotalMonthlyPayment = totalMonthly,
                TotalDebts = debts.Count
            };
        }

        public async Task<DailyInterestResult> GetDailyInterestAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var debts = (await debtRepository.GetAllAsync(userId))
                .Where(d => d.CurrentBalance > 0)
                .ToList();

            var items = debts.Select(d =>
            {
                var daily = d.CurrentBalance * (d.InterestRate / 100m) / 365m;
                var monthly = daily * 30;
                var yearly = daily * 365;

                return new DailyInterestItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    CurrentBalance = d.CurrentBalance,
                    InterestRate = d.InterestRate,
                    DailyInterest = Math.Round(daily, 2),
                    MonthlyInterest = Math.Round(monthly, 2),
                    YearlyInterest = Math.Round(yearly, 2)
                };
            }).ToList();

            return new DailyInterestResult
            {
                Items = items,
                TotalDaily = Math.Round(items.Sum(i => i.DailyInterest), 2),
                TotalMonthly = Math.Round(items.Sum(i => i.MonthlyInterest), 2),
                TotalYearly = Math.Round(items.Sum(i => i.YearlyInterest), 2)
            };
        }

        private static PayoffStep ToPayoffStep(domain.Entities.Debt d, int order)
        {
            var months = d.MonthlyPayment > 0
                ? (int)Math.Ceiling(d.CurrentBalance / d.MonthlyPayment)
                : 0;

            return new PayoffStep
            {
                Id = d.Id,
                Name = d.Name,
                CurrentBalance = d.CurrentBalance,
                InterestRate = d.InterestRate,
                MonthlyPayment = d.MonthlyPayment,
                PayoffOrder = order,
                EstimatedMonths = months,
                EstimatedPayoffDate = months > 0 ? DateTime.UtcNow.AddMonths(months) : null
            };
        }
    }
}

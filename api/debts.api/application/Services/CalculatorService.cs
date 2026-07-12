using application.Models;
using persistence.Repositories;

namespace application.Services
{
    public interface ICalculatorService
    {
        Task<PayoffStrategyResult> GetPayoffStrategyAsync(Guid userId, decimal monthlyPayment = 0);
        Task<DailyInterestResult> GetDailyInterestAsync(Guid userId);
        Task<PrepaymentAnalysisResult> AnalyzePrepaymentAsync(Guid userId, PrepaymentAnalysisRequest request);
    }

    public class CalculatorService : ICalculatorService
    {
        private readonly IDebtRepository debtRepository;

        public CalculatorService(IDebtRepository debtRepository)
        {
            this.debtRepository = debtRepository;
        }

        public async Task<PayoffStrategyResult> GetPayoffStrategyAsync(Guid userId, decimal monthlyPayment = 0)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            var debts = (await debtRepository.GetAllAsync(userId))
                .Where(d => d.CurrentBalance > 0)
                .ToList();

            if (monthlyPayment <= 0)
                monthlyPayment = debts.Sum(d => d.MonthlyPayment);

            if (monthlyPayment <= 0)
            {
                return new PayoffStrategyResult();
            }

            var snowball = SimulatePayoff(debts.OrderBy(d => d.CurrentBalance).ToList(), monthlyPayment);
            var avalanche = SimulatePayoff(debts.OrderByDescending(d => d.InterestRate).ThenBy(d => d.CurrentBalance).ToList(), monthlyPayment);

            return new PayoffStrategyResult
            {
                Snowball = snowball,
                Avalanche = avalanche,
                SnowballMonths = snowball.Count,
                AvalancheMonths = avalanche.Count
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
                    DebtName = d.Name,
                    CurrentBalance = d.CurrentBalance,
                    InterestRate = d.InterestRate,
                    DailyInterest = Math.Round(daily, 2),
                    MonthlyInterest = Math.Round(monthly, 2),
                    YearlyInterest = Math.Round(yearly, 2)
                };
            }).ToList();

            var totalDaily = Math.Round(items.Sum(i => i.DailyInterest), 2);

            return new DailyInterestResult
            {
                Items = items,
                TotalDaily = totalDaily,
                TotalMonthly = Math.Round(items.Sum(i => i.MonthlyInterest), 2),
                TotalYearly = Math.Round(items.Sum(i => i.YearlyInterest), 2),
                TotalDailyInterest = totalDaily
            };
        }

        public async Task<PrepaymentAnalysisResult> AnalyzePrepaymentAsync(Guid userId, PrepaymentAnalysisRequest request)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("UserId is required");

            if (request.ExtraAmount <= 0)
                throw new ArgumentException("ExtraAmount must be greater than zero");

            var debt = await debtRepository.GetByIdAsync(request.DebtId);
            if (debt == null || debt.UserId != userId)
                throw new KeyNotFoundException("Debt not found");

            var balance = debt.CurrentBalance;
            var payment = debt.MonthlyPayment;
            var monthlyRate = debt.InterestRate / 100m / 12m;

            var currentMonths = payment > 0 && monthlyRate > 0
                ? CalculateRemainingMonths(balance, payment, monthlyRate)
                : 0;

            var currentInterest = currentMonths > 0
                ? currentMonths * payment - balance
                : 0m;

            var newBalance = Math.Max(0, balance - request.ExtraAmount);

            // Scenario 1: same monthly payment, reduce term
            PrepaymentScenario reduceTerm;
            if (newBalance <= 0)
            {
                reduceTerm = new PrepaymentScenario
                {
                    RemainingMonths = 0,
                    NewMonthlyPayment = 0,
                    TotalInterest = 0,
                    InterestSaved = currentInterest,
                    MonthsSaved = currentMonths,
                    PayoffDate = DateTime.UtcNow
                };
            }
            else if (payment > 0 && monthlyRate > 0)
            {
                var newMonths = CalculateRemainingMonths(newBalance, payment, monthlyRate);
                var newInterest = newMonths * payment - newBalance;
                reduceTerm = new PrepaymentScenario
                {
                    RemainingMonths = newMonths,
                    NewMonthlyPayment = payment,
                    TotalInterest = Math.Max(0, newInterest),
                    InterestSaved = Math.Max(0, currentInterest - newInterest),
                    MonthsSaved = currentMonths - newMonths,
                    PayoffDate = DateTime.UtcNow.AddMonths(newMonths)
                };
            }
            else
            {
                reduceTerm = new PrepaymentScenario();
            }

            // Scenario 2: same term, reduce monthly payment
            PrepaymentScenario reducePayment;
            if (newBalance <= 0)
            {
                reducePayment = new PrepaymentScenario
                {
                    RemainingMonths = 0,
                    NewMonthlyPayment = 0,
                    TotalInterest = 0,
                    InterestSaved = currentInterest,
                    MonthsSaved = currentMonths,
                    PayoffDate = DateTime.UtcNow
                };
            }
            else if (currentMonths > 0 && monthlyRate > 0)
            {
                var newPayment = CalculatePaymentForTerm(newBalance, currentMonths, monthlyRate);
                var newInterest = currentMonths * newPayment - newBalance;
                reducePayment = new PrepaymentScenario
                {
                    RemainingMonths = currentMonths,
                    NewMonthlyPayment = Math.Round(newPayment, 2),
                    TotalInterest = Math.Max(0, newInterest),
                    InterestSaved = Math.Max(0, currentInterest - newInterest),
                    MonthsSaved = 0,
                    PayoffDate = DateTime.UtcNow.AddMonths(currentMonths)
                };
            }
            else
            {
                reducePayment = new PrepaymentScenario();
            }

            return new PrepaymentAnalysisResult
            {
                DebtName = debt.Name,
                CurrentBalance = balance,
                ExtraAmount = request.ExtraAmount,
                NewBalance = newBalance,
                CurrentMonthlyPayment = payment,
                CurrentRemainingMonths = currentMonths,
                CurrentTotalInterest = Math.Max(0, currentInterest),
                ReduceTerm = reduceTerm,
                ReducePayment = reducePayment
            };
        }

        private static int CalculateRemainingMonths(decimal balance, decimal monthlyPayment, decimal monthlyRate)
        {
            if (monthlyPayment <= balance * monthlyRate)
                return 0;

            var n = (double)(monthlyPayment / (monthlyPayment - balance * monthlyRate));
            var logN = Math.Log((double)n);
            var log1r = Math.Log(1 + (double)monthlyRate);
            var months = (int)Math.Ceiling(logN / log1r);
            return Math.Max(0, months);
        }

        private static decimal CalculatePaymentForTerm(decimal balance, int months, decimal monthlyRate)
        {
            var r = (double)monthlyRate;
            var n = (double)months;
            var factor = r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1);
            return (decimal)factor * balance;
        }

        private static List<PayoffStep> SimulatePayoff(List<domain.Entities.Debt> orderedDebts, decimal monthlyPayment)
        {
            var steps = new List<PayoffStep>();
            var balances = orderedDebts.ToDictionary(d => d.Id, d => d.CurrentBalance);
            var month = 0;

            while (balances.Values.Any(b => b > 0))
            {
                month++;
                var remaining = monthlyPayment;

                foreach (var debt in orderedDebts)
                {
                    if (remaining <= 0) break;
                    if (balances[debt.Id] <= 0) continue;

                    var payment = Math.Min(remaining, balances[debt.Id]);
                    balances[debt.Id] -= payment;
                    remaining -= payment;

                    steps.Add(new PayoffStep
                    {
                        Month = month,
                        DebtName = debt.Name,
                        PaymentAmount = payment,
                        RemainingBalance = balances[debt.Id]
                    });
                }

                if (month > 1200) break;
            }

            return steps;
        }
    }
}

using application.Services;
using domain.Entities;
using Moq;
using persistence.Repositories;

namespace tests;

[TestClass]
public class CalculatorServiceTests
{
    private readonly Mock<IDebtRepository> repoMock = new();
    private readonly CalculatorService service;

    public CalculatorServiceTests()
    {
        service = new CalculatorService(repoMock.Object);
    }

    private static Debt MakeDebt(decimal balance, decimal rate, decimal payment, string name = "Test") => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Name = name,
        OriginalAmount = balance,
        CurrentBalance = balance,
        InterestRate = rate,
        MonthlyPayment = payment,
        StartDate = DateTime.UtcNow.AddDays(-30),
        DueDate = DateTime.UtcNow.AddDays(30)
    };

    [TestMethod]
    public async Task GetPayoffStrategyAsync_ReturnsBothStrategies()
    {
        var userId = Guid.NewGuid();
        var debts = new[]
        {
            MakeDebt(5000, 10, 200, "A"),
            MakeDebt(1000, 5, 100, "B"),
            MakeDebt(3000, 15, 150, "C")
        };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.TotalDebts);
        Assert.AreEqual(450, result.TotalMonthlyPayment);
        Assert.AreEqual(3, result.Snowball.Count);
        Assert.AreEqual(3, result.Avalanche.Count);

        // Snowball: sorted by balance ascending → B, C, A
        Assert.AreEqual(1000, result.Snowball[0].CurrentBalance);
        Assert.AreEqual(3000, result.Snowball[1].CurrentBalance);
        Assert.AreEqual(5000, result.Snowball[2].CurrentBalance);

        // Avalanche: sorted by rate descending → C (15%), A (10%), B (5%)
        Assert.AreEqual(15, result.Avalanche[0].InterestRate);
        Assert.AreEqual(10, result.Avalanche[1].InterestRate);
        Assert.AreEqual(5, result.Avalanche[2].InterestRate);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_Months_CalculatedCorrectly()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { MakeDebt(1000, 5, 250) };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetPayoffStrategyAsync(userId);

        // 1000 / 250 = 4 months
        Assert.AreEqual(4, result.Snowball[0].EstimatedMonths);
        Assert.IsNotNull(result.Snowball[0].EstimatedPayoffDate);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_ZeroPayment_ReturnsZeroMonths()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { MakeDebt(1000, 5, 0) };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.AreEqual(0, result.Snowball[0].EstimatedMonths);
        Assert.IsNull(result.Snowball[0].EstimatedPayoffDate);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_NoActiveDebts_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(Array.Empty<Debt>());

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.AreEqual(0, result.TotalDebts);
        Assert.AreEqual(0, result.Snowball.Count);
        Assert.AreEqual(0, result.Avalanche.Count);
        Assert.AreEqual(0, result.TotalMonthlyPayment);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetPayoffStrategyAsync_EmptyUserId_Throws() =>
        await service.GetPayoffStrategyAsync(Guid.Empty);

    [TestMethod]
    public async Task GetDailyInterestAsync_ReturnsItemsWithTotals()
    {
        var userId = Guid.NewGuid();
        var debts = new[]
        {
            MakeDebt(10000, 5, 500, "Debt1"),
            MakeDebt(5000, 10, 300, "Debt2")
        };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetDailyInterestAsync(userId);

        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Items.Count);

        // Debt1: 10000 * 0.05 / 365 = 1.3698...
        // Debt2: 5000 * 0.10 / 365 = 1.3698...
        // So total daily ≈ 2.74
        Assert.IsTrue(result.TotalDaily > 0);
        Assert.IsTrue(result.TotalMonthly > 0);
        Assert.IsTrue(result.TotalYearly > 0);
    }

    [TestMethod]
    public async Task GetDailyInterestAsync_NoActiveDebts_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(Array.Empty<Debt>());

        var result = await service.GetDailyInterestAsync(userId);

        Assert.AreEqual(0, result.Items.Count);
        Assert.AreEqual(0, result.TotalDaily);
        Assert.AreEqual(0, result.TotalMonthly);
        Assert.AreEqual(0, result.TotalYearly);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetDailyInterestAsync_EmptyUserId_Throws() =>
        await service.GetDailyInterestAsync(Guid.Empty);
}

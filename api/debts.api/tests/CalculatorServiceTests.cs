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
        Assert.IsTrue(result.Snowball.Count > 0);
        Assert.IsTrue(result.Avalanche.Count > 0);
        Assert.IsTrue(result.SnowballMonths > 0);
        Assert.IsTrue(result.AvalancheMonths > 0);

        // Snowball: B paid first (lowest balance)
        Assert.AreEqual("B", result.Snowball[0].DebtName);
        Assert.AreEqual(1, result.Snowball[0].Month);
        Assert.AreEqual(450, result.Snowball[0].PaymentAmount);

        // Avalanche: C paid first (highest rate)
        Assert.AreEqual("C", result.Avalanche[0].DebtName);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_Months_CalculatedCorrectly()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { MakeDebt(1000, 5, 250) };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.AreEqual(4, result.Snowball.Count);
        Assert.AreEqual(4, result.SnowballMonths);
        Assert.AreEqual("Test", result.Snowball[0].DebtName);
        Assert.AreEqual(250, result.Snowball[0].PaymentAmount);
        Assert.AreEqual(750, result.Snowball[0].RemainingBalance);
        Assert.AreEqual(0, result.Snowball[^1].RemainingBalance);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_ZeroMonthlyPayment_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { MakeDebt(1000, 5, 0) };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.AreEqual(0, result.Snowball.Count);
        Assert.AreEqual(0, result.Avalanche.Count);
        Assert.AreEqual(0, result.SnowballMonths);
        Assert.AreEqual(0, result.AvalancheMonths);
    }

    [TestMethod]
    public async Task GetPayoffStrategyAsync_NoActiveDebts_ReturnsEmpty()
    {
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(Array.Empty<Debt>());

        var result = await service.GetPayoffStrategyAsync(userId);

        Assert.AreEqual(0, result.Snowball.Count);
        Assert.AreEqual(0, result.Avalanche.Count);
        Assert.AreEqual(0, result.SnowballMonths);
        Assert.AreEqual(0, result.AvalancheMonths);
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

        Assert.IsTrue(result.TotalDaily > 0);
        Assert.IsTrue(result.TotalMonthly > 0);
        Assert.IsTrue(result.TotalYearly > 0);
        Assert.AreEqual(result.TotalDaily, result.TotalDailyInterest);
        Assert.AreEqual("Debt1", result.Items[0].DebtName);
        Assert.AreEqual("Debt2", result.Items[1].DebtName);
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
        Assert.AreEqual(0, result.TotalDailyInterest);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetDailyInterestAsync_EmptyUserId_Throws() =>
        await service.GetDailyInterestAsync(Guid.Empty);
}

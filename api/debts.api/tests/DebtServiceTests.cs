using application.Services;
using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using persistence.Repositories;

namespace tests;

[TestClass]
public class DebtServiceTests
{
    private readonly Mock<IDebtRepository> repoMock = new();
    private readonly DebtService service;

    public DebtServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:ExpirationMinutes"] = "60" })
            .Build();
        service = new DebtService(repoMock.Object, cache, config);
    }

    private static Debt ValidDebt => new()
    {
        UserId = Guid.NewGuid(),
        Name = "Test Debt",
        OriginalAmount = 10000,
        CurrentBalance = 8000,
        InterestRate = 5.5m,
        MonthlyPayment = 500,
        StartDate = DateTime.UtcNow.AddDays(-30),
        DueDate = DateTime.UtcNow.AddDays(30)
    };

    [TestMethod]
    public async Task CreateDebtAsync_ValidDebt_ReturnsCreatedDebt()
    {
        var debt = ValidDebt;
        repoMock.Setup(r => r.CreateDebtAsync(It.IsAny<Debt>())).ReturnsAsync(debt);

        var result = await service.CreateDebtAsync(debt);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        Assert.AreEqual(debt.Name, result.Name);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateDebtAsync_EmptyUserId_Throws() =>
        await service.CreateDebtAsync(new Debt { Name = "Test", OriginalAmount = 100, CurrentBalance = 80, InterestRate = 5, MonthlyPayment = 10, StartDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30) });

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateDebtAsync_EmptyName_Throws()
    {
        var debt = ValidDebt;
        debt.Name = "";
        await service.CreateDebtAsync(debt);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateDebtAsync_NegativeOriginalAmount_Throws()
    {
        var debt = ValidDebt;
        debt.OriginalAmount = -1;
        await service.CreateDebtAsync(debt);
    }

    [TestMethod]
    public async Task GetByIdAsync_Existing_ReturnsDebt()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var debt = ValidDebt;
        debt.Id = id;
        debt.UserId = userId;
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(debt);

        var result = await service.GetByIdAsync(id, userId);

        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Debt?)null);

        var result = await service.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetByIdAsync_EmptyId_Throws() =>
        await service.GetByIdAsync(Guid.Empty, Guid.NewGuid());

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetByIdAsync_EmptyUserId_Throws() =>
        await service.GetByIdAsync(Guid.NewGuid(), Guid.Empty);

    [TestMethod]
    public async Task GetAllDebtsAsync_ReturnsDebts()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { ValidDebt, ValidDebt };
        repoMock.Setup(r => r.GetAllAsync(userId)).ReturnsAsync(debts);

        var result = await service.GetAllDebtsAsync(userId);

        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetAllDebtsAsync_EmptyUserId_Throws() =>
        await service.GetAllDebtsAsync(Guid.Empty);

    [TestMethod]
    public async Task DeleteDebtAsync_Existing_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.DeleteDebtAsync(id, userId)).ReturnsAsync(true);

        var result = await service.DeleteDebtAsync(id, userId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task DeleteDebtAsync_EmptyId_Throws() =>
        await service.DeleteDebtAsync(Guid.Empty, Guid.NewGuid());

    [TestMethod]
    public async Task PayOffDebtAsync_Existing_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.PayOffDebtAsync(id, userId)).ReturnsAsync(true);

        var result = await service.PayOffDebtAsync(id, userId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task PayOffDebtAsync_EmptyId_Throws() =>
        await service.PayOffDebtAsync(Guid.Empty, Guid.NewGuid());

    [TestMethod]
    public async Task SearchDebtsAsync_ReturnsMatching()
    {
        var userId = Guid.NewGuid();
        var debts = new[] { ValidDebt };
        repoMock.Setup(r => r.SearchDebtsAsync(userId, "test")).ReturnsAsync(debts);

        var result = await service.SearchDebtsAsync(userId, "test");

        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task SearchDebtsAsync_EmptyQuery_Throws() =>
        await service.SearchDebtsAsync(Guid.NewGuid(), "");

    [TestMethod]
    public async Task GetOverdueDebtsAsync_ReturnsOverdue()
    {
        var userId = Guid.NewGuid();
        repoMock.Setup(r => r.GetOverdueDebtsAsync(userId)).ReturnsAsync(Array.Empty<Debt>());

        var result = await service.GetOverdueDebtsAsync(userId);

        Assert.IsNotNull(result);
    }
}

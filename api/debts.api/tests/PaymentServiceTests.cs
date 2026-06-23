using application.Services;
using domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using persistence.Models;
using persistence.Repositories;

namespace tests;

[TestClass]
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> paymentRepoMock = new();
    private readonly Mock<IDebtRepository> debtRepoMock = new();
    private readonly PaymentService service;

    public PaymentServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:ExpirationMinutes"] = "60" })
            .Build();
        service = new PaymentService(paymentRepoMock.Object, debtRepoMock.Object, cache, config);
    }

    [TestMethod]
    public async Task CreatePaymentAsync_Valid_ReturnsPayment()
    {
        var debtId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var payment = new Payment { Id = Guid.NewGuid(), DebtId = debtId, Amount = 500, PaymentDate = DateTime.UtcNow };
        debtRepoMock.Setup(r => r.GetByIdAsync(debtId)).ReturnsAsync(new Debt { Id = debtId, UserId = userId });
        paymentRepoMock.Setup(r => r.CreatePaymentAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var result = await service.CreatePaymentAsync(debtId, 500);

        Assert.IsNotNull(result);
        Assert.AreEqual(debtId, result.DebtId);
        Assert.AreEqual(500, result.Amount);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreatePaymentAsync_EmptyDebtId_Throws() =>
        await service.CreatePaymentAsync(Guid.Empty, 100);

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreatePaymentAsync_ZeroAmount_Throws() =>
        await service.CreatePaymentAsync(Guid.NewGuid(), 0);

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreatePaymentAsync_NegativeAmount_Throws() =>
        await service.CreatePaymentAsync(Guid.NewGuid(), -10);

    [TestMethod]
    public async Task GetByIdAsync_Existing_ReturnsPayment()
    {
        var id = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Payment { Id = id });

        var result = await service.GetByIdAsync(id);

        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        paymentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetByIdAsync_EmptyId_Throws() =>
        await service.GetByIdAsync(Guid.Empty);

    [TestMethod]
    public async Task GetByDebtIdAsync_ReturnsPayments()
    {
        var debtId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetByDebtIdAsync(debtId)).ReturnsAsync(new[] { new Payment() });

        var result = await service.GetByDebtIdAsync(debtId);

        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    public async Task DeletePaymentAsync_Existing_ReturnsTrue()
    {
        var paymentId = Guid.NewGuid();
        var debtId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(new Payment { Id = paymentId, DebtId = debtId });
        debtRepoMock.Setup(r => r.GetByIdAsync(debtId)).ReturnsAsync(new Debt { Id = debtId, UserId = Guid.NewGuid() });
        paymentRepoMock.Setup(r => r.DeletePaymentAsync(paymentId)).ReturnsAsync(true);

        var result = await service.DeletePaymentAsync(paymentId);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task DeletePaymentAsync_EmptyId_Throws() =>
        await service.DeletePaymentAsync(Guid.Empty);

    [TestMethod]
    public async Task UpdatePaymentAsync_Existing_ReturnsTrue()
    {
        var paymentId = Guid.NewGuid();
        var debtId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(new Payment { Id = paymentId, DebtId = debtId, Amount = 200 });
        debtRepoMock.Setup(r => r.GetByIdAsync(debtId)).ReturnsAsync(new Debt { Id = debtId, UserId = Guid.NewGuid() });
        paymentRepoMock.Setup(r => r.UpdatePaymentAsync(paymentId, 300)).ReturnsAsync(true);

        var result = await service.UpdatePaymentAsync(paymentId, 300);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task UpdatePaymentAsync_EmptyId_Throws() =>
        await service.UpdatePaymentAsync(Guid.Empty, 100);

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task UpdatePaymentAsync_ZeroAmount_Throws() =>
        await service.UpdatePaymentAsync(Guid.NewGuid(), 0);

    [TestMethod]
    public async Task GetPaymentHistoryAsync_ReturnsHistory()
    {
        var userId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetPaymentHistoryByUserAsync(userId)).ReturnsAsync(new[] { new Payment() });

        var result = await service.GetPaymentHistoryAsync(userId);

        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    public async Task GetPaymentHistoryWithDebtNameAsync_ReturnsHistory()
    {
        var userId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetPaymentHistoryWithDebtNameAsync(userId)).ReturnsAsync(new[] { new PaymentWithDebtName() });

        var result = await service.GetPaymentHistoryWithDebtNameAsync(userId);

        Assert.AreEqual(1, result.Count());
    }

    [TestMethod]
    public async Task GetTotalByDebtAsync_ReturnsTotal()
    {
        var debtId = Guid.NewGuid();
        paymentRepoMock.Setup(r => r.GetTotalByDebtAsync(debtId)).ReturnsAsync(1500);

        var result = await service.GetTotalByDebtAsync(debtId);

        Assert.AreEqual(1500, result);
    }
}

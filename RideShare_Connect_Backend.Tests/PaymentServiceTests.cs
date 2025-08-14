using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;
using RideShare_Connect.Models.PaymentManagement;

namespace RideShare_Connect_Backend.Tests;

public class PaymentServiceTests
{
    private class FakeGateway : IPaymentGateway
    {
        public Task<PaymentGatewayResult> CreateChargeAsync(PaymentProcessDto dto)
            => Task.FromResult(new PaymentGatewayResult { TransactionId = "txn_123", Status = "Pending" });

        public Task<string> GetPaymentStatusAsync(string transactionId)
            => Task.FromResult("Completed");

        public Task<CardTokenResult> TokenizeCardAsync(string cardNumber, DateTime expiryDate)
            => Task.FromResult(new CardTokenResult { Token = "tok_test", Last4 = cardNumber[^4..], Brand = "Visa" });
    }

    [Test]
    public async Task ProcessPaymentAsync_StoresGatewayTransaction()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context, new FakeGateway());
        var dto = new PaymentProcessDto
        {
            UserId = 1,
            BookingId = 1,
            Amount = 20m,
            PaymentMethodId = 1
        };

        // Act
        var result = await service.ProcessPaymentAsync(dto);

        // Assert
        Assert.That(result.Status, Is.EqualTo("Pending"));
        Assert.That(result.ProviderTransactionId, Is.EqualTo("txn_123"));
        Assert.That(await context.Payments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdatePaymentStatusAsync_UpdatesStatus()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context, new FakeGateway());
        var dto = new PaymentProcessDto
        {
            UserId = 1,
            BookingId = 1,
            Amount = 20m,
            PaymentMethodId = 1
        };

        var payment = await service.ProcessPaymentAsync(dto);
        await service.UpdatePaymentStatusAsync(payment.ProviderTransactionId!);

        var updated = await context.Payments.FindAsync(payment.Id);
        Assert.That(updated!.Status, Is.EqualTo("Completed"));
    }

    [Test]
    public async Task AddMoneyAsync_CreatesWallet_WhenNoneExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context, new FakeGateway());
        var dto = new AddMoneyDto { UserId = 1, Amount = 50m };

        // Act
        var wallet = await service.AddMoneyAsync(dto);

        // Assert
        Assert.That(wallet.Balance, Is.EqualTo(50m));
        Assert.That(await context.Wallets.CountAsync(), Is.EqualTo(1));
        Assert.That(await context.WalletTransactions.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetWalletBalanceAsync_ReturnsZero_WhenWalletMissing()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context, new FakeGateway());

        // Act
        var balance = await service.GetWalletBalanceAsync(1);

        // Assert
        Assert.That(balance, Is.EqualTo(0m));
    }
}

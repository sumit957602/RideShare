using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;
using RideShare_Connect.Models.PaymentManagement;

namespace RideShare_Connect_Backend.Tests;

public class PaymentServiceTests
{
    [Test]
    public async Task ProcessPaymentAsync_AddsPaymentWithCompletedStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context);
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
        Assert.That(result.Status, Is.EqualTo("Completed"));
        Assert.That(await context.Payments.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task ConfirmPaymentAsync_ReturnsNull_WhenPaymentNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context);
        var dto = new PaymentConfirmDto { PaymentId = 99 };

        // Act
        var result = await service.ConfirmPaymentAsync(dto);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddMoneyAsync_CreatesWallet_WhenNoneExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new PaymentService(context);
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
        var service = new PaymentService(context);

        // Act
        var balance = await service.GetWalletBalanceAsync(1);

        // Assert
        Assert.That(balance, Is.EqualTo(0m));
    }
}

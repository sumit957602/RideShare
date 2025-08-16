using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RideShare_Connect.Api.Controllers;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect_Backend.Tests;

public class RideBookingsControllerTests
{
    [Test]
    public async Task AcceptRide_ValidRequest_CreatesBookingPaymentAndTransactionSummary()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Driver.Add(new Driver { DriverId = 1, Email = "d@test.com", PasswordHash = "hash", FullName = "Driver", PhoneNumber = "9123456789" });
        context.Vehicles.Add(new Vehicle { Id = 1, DriverId = 1, CarMaker = "Toyota", Model = "Camry", VehicleType = "Sedan", LicensePlate = "ABC123", VerificationStatus = "Pending", Year = 2020 });
        var ride = new Ride
        {
            Id = 1,
            DriverId = 1,
            VehicleId = 1,
            Origin = "O",
            Destination = "D",
            DepartureTime = DateTime.UtcNow.AddHours(1),
            TotalSeats = 3,
            AvailableSeats = 3,
            PricePerSeat = 10m,
            DistanceKm = 10,
            Status = "Scheduled"
        };
        context.Rides.Add(ride);
        await context.SaveChangesAsync();
        var controller = new RideBookingsController(context);
        var dto = new RideBookingRequestDto
        {
            RideId = 1,
            UserId = 1,
            NumPersons = 2,
            PaymentMode = "Cash",
            PickupLocation = "A",
            DropLocation = "B",
            DistanceKm = 5
        };

        // Act
        var result = await controller.AcceptRide(dto);

        // Assert
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(await context.RideBookings.CountAsync(), Is.EqualTo(1));
        Assert.That(await context.Payments.CountAsync(), Is.EqualTo(1));
        var payment = await context.Payments.FirstAsync();
        Assert.That(payment.Amount, Is.EqualTo(100));
        Assert.That(payment.PaymentMode, Is.EqualTo("Cash"));
        Assert.That(await context.UserTransactionSummaries.CountAsync(), Is.EqualTo(1));
        var summary = await context.UserTransactionSummaries.FirstAsync();
        Assert.That(summary.TotalAmount, Is.EqualTo(100));
        Assert.That(summary.TotalTransactionAmount, Is.EqualTo(0));
        Assert.That((await context.Rides.FirstAsync()).AvailableSeats, Is.EqualTo(1));
        var totalFare = (decimal)ok.Value!.GetType().GetProperty("totalFare")!.GetValue(ok.Value)!;
        Assert.That(totalFare, Is.EqualTo(100));
    }

    [Test]
    public async Task AcceptRide_NonCashRequest_UpdatesTransactionSummaryWithTransactionAmount()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Driver.Add(new Driver { DriverId = 1, Email = "d@test.com", PasswordHash = "hash", FullName = "Driver", PhoneNumber = "9123456789" });
        context.Vehicles.Add(new Vehicle { Id = 1, DriverId = 1, CarMaker = "Toyota", Model = "Camry", VehicleType = "Sedan", LicensePlate = "ABC123", VerificationStatus = "Pending", Year = 2020 });
        var ride = new Ride
        {
            Id = 1,
            DriverId = 1,
            VehicleId = 1,
            Origin = "O",
            Destination = "D",
            DepartureTime = DateTime.UtcNow.AddHours(1),
            TotalSeats = 3,
            AvailableSeats = 3,
            PricePerSeat = 10m,
            DistanceKm = 10,
            Status = "Scheduled"
        };
        context.Rides.Add(ride);
        await context.SaveChangesAsync();
        var controller = new RideBookingsController(context);
        var dto = new RideBookingRequestDto
        {
            RideId = 1,
            UserId = 1,
            NumPersons = 2,
            PaymentMode = "Wallet",
            PickupLocation = "A",
            DropLocation = "B",
            DistanceKm = 5
        };

        var result = await controller.AcceptRide(dto);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        Assert.That(await context.UserTransactionSummaries.CountAsync(), Is.EqualTo(1));
        var summary = await context.UserTransactionSummaries.FirstAsync();
        Assert.That(summary.TotalAmount, Is.EqualTo(100));
        Assert.That(summary.TotalTransactionAmount, Is.EqualTo(100));
    }

    [Test]
    public async Task AcceptRide_SubsequentBookings_UpdateExistingTransactionSummary()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Driver.Add(new Driver { DriverId = 1, Email = "d@test.com", PasswordHash = "hash", FullName = "Driver", PhoneNumber = "9123456789" });
        context.Vehicles.Add(new Vehicle { Id = 1, DriverId = 1, CarMaker = "Toyota", Model = "Camry", VehicleType = "Sedan", LicensePlate = "ABC123", VerificationStatus = "Pending", Year = 2020 });
        var ride = new Ride
        {
            Id = 1,
            DriverId = 1,
            VehicleId = 1,
            Origin = "O",
            Destination = "D",
            DepartureTime = DateTime.UtcNow.AddHours(1),
            TotalSeats = 3,
            AvailableSeats = 3,
            PricePerSeat = 10m,
            DistanceKm = 10,
            Status = "Scheduled"
        };
        context.Rides.Add(ride);
        await context.SaveChangesAsync();
        var controller = new RideBookingsController(context);

        var cashDto = new RideBookingRequestDto
        {
            RideId = 1,
            UserId = 1,
            NumPersons = 1,
            PaymentMode = "Cash",
            PickupLocation = "A",
            DropLocation = "B",
            DistanceKm = 5
        };
        await controller.AcceptRide(cashDto);

        var walletDto = new RideBookingRequestDto
        {
            RideId = 1,
            UserId = 1,
            NumPersons = 1,
            PaymentMode = "Wallet",
            PickupLocation = "A2",
            DropLocation = "B2",
            DistanceKm = 5
        };
        await controller.AcceptRide(walletDto);

        Assert.That(await context.UserTransactionSummaries.CountAsync(), Is.EqualTo(1));
        var summary = await context.UserTransactionSummaries.FirstAsync();
        Assert.That(summary.TotalAmount, Is.EqualTo(100));
        Assert.That(summary.TotalTransactionAmount, Is.EqualTo(50));
    }

    [Test]
    public async Task AcceptRide_RideNotFound_ReturnsNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var context = new ApplicationDbContext(options);
        var controller = new RideBookingsController(context);
        var dto = new RideBookingRequestDto
        {
            RideId = 99,
            UserId = 1,
            NumPersons = 1,
            PaymentMode = "Wallet",
            PickupLocation = "A",
            DropLocation = "B",
            DistanceKm = 5
        };

        // Act
        var result = await controller.AcceptRide(dto);

        // Assert
        Assert.IsInstanceOf<NotFoundObjectResult>(result);
    }
}

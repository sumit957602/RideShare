using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RideShare_Connect.Api.Controllers;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect_Backend.Tests;

public class VehiclesControllerTests
{
    [Test]
    public async Task RegisterVehicle_CreatesVehicle()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Driver.Add(new Driver { DriverId = 1, Email = "d@test.com", PasswordHash = "hash", FullName = "Driver", PhoneNumber = "9123456789" });
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher<Driver>>();
        var controller = new VehiclesController(context, passwordHasher.Object);
        var dto = new VehicleRegisterDto
        {
            CarMaker = "Toyota",
            CarModel = "Camry",
            VehicleType = "Sedan",
            LicensePlate = "ABC123",
            Year = 2020
        };

        // Act
        var result = await controller.RegisterVehicle(1, dto);

        // Assert
        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(await context.Vehicles.CountAsync(), Is.EqualTo(1));
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using RideShare_Connect.Api.Controllers;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect_Backend.Services;

namespace RideShare_Connect_Backend.Tests;

public class DriversControllerTests
{
    [Test]
    public async Task RegisterDriver_CreatesDriverAndProfile()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);

        var passwordHasher = new Mock<IPasswordHasher<Driver>>();
        passwordHasher.Setup(h => h.HashPassword(It.IsAny<Driver>(), It.IsAny<string>()))
            .Returns("hashed");
        var configuration = new ConfigurationBuilder().Build();
        var emailService = new Mock<IEmailService>();
        var cache = new Mock<IDistributedCache>();

        var controller = new DriversController(
            context,
            passwordHasher.Object,
            configuration,
            emailService.Object,
            cache.Object
        );

        var dto = new DriverRegisterDto
        {
            Email = "driver@test.com",
            PhoneNumber = "9123456789",
            Password = "Password1!",
            ConfirmPassword = "Password1!",
            FullName = "Test Driver",
            LicenseNumber = "LIC123",
            DrivingExperienceYears = 5,
            DOB = DateTime.UtcNow.AddYears(-30)
        };

        // Act
        var result = await controller.RegisterDriver(dto);

        // Assert
        var created = result.Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(await context.Driver.CountAsync(), Is.EqualTo(1));
        Assert.That(await context.DriverProfiles.CountAsync(), Is.EqualTo(1));
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using RideShare_Connect.Api.Controllers;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect_Backend.Services;
using System.Linq;

namespace RideShare_Connect_Backend.Tests;

public class UsersControllerTests
{
    [Test]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Users.AddRange(
            new User { Email = "a@test.com", PasswordHash = "hash", PhoneNumber = "9123456789", UserType = "Rider", AccountStatus = "Active" },
            new User { Email = "b@test.com", PasswordHash = "hash", PhoneNumber = "9234567890", UserType = "Rider", AccountStatus = "Active" }
        );
        await context.SaveChangesAsync();

        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var configuration = new ConfigurationBuilder().Build();
        var environment = new Mock<IWebHostEnvironment>();
        var emailService = new Mock<IEmailService>();
        var cache = new Mock<IDistributedCache>();

        var controller = new UsersController(
            context,
            passwordHasher.Object,
            configuration,
            environment.Object,
            emailService.Object,
            cache.Object
        );

        // Act
        var result = await controller.GetUsers();

        // Assert
        var okResult = result.Result as OkObjectResult;
        var users = okResult?.Value as IEnumerable<UserSummaryDto>;
        Assert.That(users?.Count(), Is.EqualTo(2));
    }
}

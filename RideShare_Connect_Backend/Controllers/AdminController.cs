using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.AdminManagement;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalRides = await _context.Rides.CountAsync();
            var totalDrivers = await _context.Users.CountAsync(u => u.UserType == "Driver");
            var totalVehicles = await _context.Vehicles.CountAsync();
            return Ok(new { totalUsers, totalRides, totalDrivers, totalVehicles });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.Email, u.UserType, u.AccountStatus })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, UserStatusUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.AccountStatus = dto.Status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.UserReports
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportingUser)
                .ToListAsync();
            return Ok(reports);
        }

        [HttpPut("config")]
        public async Task<IActionResult> UpdateConfig(SystemConfigDto dto)
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == dto.Key);
            if (config == null)
            {
                config = new SystemConfiguration { Key = dto.Key, Value = dto.Value };
                _context.SystemConfigurations.Add(config);
            }
            else
            {
                config.Value = dto.Value;
            }
            await _context.SaveChangesAsync();
            return Ok(config);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var metrics = await _context.Analytics.ToListAsync();
            return Ok(metrics);
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification(NotificationCreateDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return NotFound("User not found");
            }

            var notification = new Notification
            {
                UserId = dto.UserId,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(CreateNotification), new { id = notification.Id }, notification);
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.UserType == "Driver")
                .Join(_context.DriverProfiles,
                      u => u.Id,
                      dp => dp.DriverId,
                      (u, dp) => new DriverDto
                      {
                          Id = u.Id,
                          Email = u.Email,
                          PhoneNumber = u.PhoneNumber,
                          FullName = u.UserProfile != null ? u.UserProfile.FullName : null,
                          UserType = u.UserType,
                          AccountStatus = u.AccountStatus,
                          CreatedAt = u.CreatedAt,
                          IsVerified = u.IsVerified,
                          VerificationBadge = u.VerificationBadge,
                          LicenseNumber = dp.LicenseNumber,
                          BackgroundCheckStatus = dp.BackgroundCheckStatus,
                          DrivingExperienceYears = dp.DrivingExperienceYears,
                          ReferenceContacts = dp.ReferenceContacts
                      }).ToListAsync();

            return Ok(drivers);
        }

        [HttpPut("drivers/{id}")]
        public async Task<IActionResult> UpdateDriver(int id, DriverUpdateDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Driver");
            if (user == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.Email != null) user.Email = dto.Email;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.AccountStatus != null) user.AccountStatus = dto.AccountStatus;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
            }
            if (dto.FullName != null) user.UserProfile.FullName = dto.FullName;

            if (dto.LicenseNumber != null) driverProfile.LicenseNumber = dto.LicenseNumber;
            if (dto.BackgroundCheckStatus != null) driverProfile.BackgroundCheckStatus = dto.BackgroundCheckStatus;
            if (dto.DrivingExperienceYears.HasValue) driverProfile.DrivingExperienceYears = dto.DrivingExperienceYears.Value;
            if (dto.ReferenceContacts != null) driverProfile.ReferenceContacts = dto.ReferenceContacts;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("drivers/{id}")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Driver");
            if (user == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile != null)
            {
                _context.DriverProfiles.Remove(driverProfile);
            }

            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == id);
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            var vehicles = await _context.Vehicles.Where(v => v.DriverId == id).ToListAsync();
            if (vehicles.Any())
            {
                _context.Vehicles.RemoveRange(vehicles);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var vehicles = await _context.Vehicles
                .Select(v => new
                {
                    v.Id,
                    v.DriverId,
                    v.CarMaker,
                    v.Model,
                    v.VehicleType,
                    v.LicensePlate,
                    v.Year,
                    v.Status
                }).ToListAsync();

            return Ok(vehicles);
        }

        [HttpPut("vehicles/{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, VehicleUpdateDto dto)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            if (dto.CarMaker != null) vehicle.CarMaker = dto.CarMaker;
            if (dto.Model != null) vehicle.Model = dto.Model;
            if (dto.VehicleType != null) vehicle.VehicleType = dto.VehicleType;
            if (dto.LicensePlate != null) vehicle.LicensePlate = dto.LicensePlate;
            if (dto.Year.HasValue) vehicle.Year = dto.Year.Value;
            if (dto.Status != null) vehicle.Status = dto.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("vehicles/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

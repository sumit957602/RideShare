using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.AdminManagement;
using RideShare_Connect.Models.UserManagement;
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
            return Ok(new { totalUsers, totalRides });
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
    }
}

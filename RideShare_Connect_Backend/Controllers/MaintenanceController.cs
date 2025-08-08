using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Driver")]
        [HttpPost("record")]
        public async Task<IActionResult> AddRecord(MaintenanceRecordDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.DriverId == driverId);
            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            var record = new MaintenanceRecord
            {
                VehicleId = dto.VehicleId,
                ServiceType = dto.ServiceType,
                ServiceDate = dto.ServiceDate,
                Details = dto.Details
            };

            _context.MaintenanceRecords.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { record.Id });
        }
    }
}

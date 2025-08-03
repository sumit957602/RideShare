using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VehiclesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Driver")]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterVehicle(VehicleRegisterDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

            var vehicle = new Vehicle
            {
                DriverId = driverId,
                CarMaker = dto.CarMaker,
                Model = dto.Model,
                VehicleType = dto.VehicleType,
                LicensePlate = dto.LicensePlate,
                Status = "Active",
                Year = dto.Year
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var result = new VehicleDto
            {
                Id = vehicle.Id,
                CarMaker = vehicle.CarMaker,
                Model = vehicle.Model,
                VehicleType = vehicle.VehicleType,
                LicensePlate = vehicle.LicensePlate,
                Year = vehicle.Year,
                Status = vehicle.Status
            };
            return CreatedAtAction(nameof(RegisterVehicle), new { id = result.Id }, result);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument([FromForm] VehicleDocumentUploadDto dto)
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

            using var ms = new MemoryStream();
            await dto.File.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            var document = new VehicleDocument
            {
                VehicleId = dto.VehicleId,
                DocumentType = dto.DocumentType,
                FileData = fileBytes,
                FileName = dto.File.FileName,
                ContentType = dto.File.ContentType,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                Status = "Uploaded"
            };
            _context.VehicleDocuments.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { document.Id, document.DocumentType });
        }

        [Authorize(Roles = "Driver")]
        [HttpGet("my-vehicles")]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

            var vehicles = await _context.Vehicles
                .Where(v => v.DriverId == driverId)
                .Select(v => new VehicleDto
                {
                    Id = v.Id,
                    CarMaker = v.CarMaker,
                    Model = v.Model,
                    VehicleType = v.VehicleType,
                    LicensePlate = v.LicensePlate,
                    Year = v.Year,
                    Status = v.Status
                }).ToListAsync();

            return Ok(vehicles);
        }
    }
}

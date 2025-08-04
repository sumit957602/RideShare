using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
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
        private readonly IPasswordHasher<Driver> _passwordHasher;

        public VehiclesController(ApplicationDbContext context, IPasswordHasher<Driver> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterVehicle([FromQuery] int driverId, [FromBody] VehicleRegisterDto dto)
        {
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

        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument([FromQuery] int driverId, [FromForm] VehicleDocumentUploadDto dto)
        {
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

        [HttpGet("my-vehicles")]
        public async Task<IActionResult> GetMyVehicles([FromQuery] int driverId)
        {
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

        [HttpPut("update")]
        public async Task<IActionResult> UpdateVehicle([FromQuery] int driverId, [FromQuery] int vehicleId, [FromBody] VehicleUpdateDto dto)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.DriverId == driverId);
            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            if (!string.IsNullOrEmpty(dto.CarMaker)) vehicle.CarMaker = dto.CarMaker;
            if (!string.IsNullOrEmpty(dto.Model)) vehicle.Model = dto.Model;
            if (!string.IsNullOrEmpty(dto.VehicleType)) vehicle.VehicleType = dto.VehicleType;
            if (!string.IsNullOrEmpty(dto.LicensePlate)) vehicle.LicensePlate = dto.LicensePlate;
            if (dto.Year.HasValue) vehicle.Year = dto.Year.Value;
            if (!string.IsNullOrEmpty(dto.Status)) vehicle.Status = dto.Status;

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

            return Ok(result);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteVehicle([FromQuery] int driverId, [FromQuery] int vehicleId, [FromBody] DeleteAccountDto dto)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (driver == null)
            {
                return NotFound("Driver not found");
            }

            var verify = _passwordHasher.VerifyHashedPassword(driver, driver.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.DriverId == driverId);
            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.RideManagement;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RidesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RidesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRides()
        {
            var rides = await _context.Rides
                .AsNoTracking()
                .Select(r => new RideDto
                {
                    Id = r.Id,
                    DriverId = r.DriverId,
                    VehicleId = r.VehicleId,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DepartureTime = r.DepartureTime,
                    TotalSeats = r.TotalSeats,
                    AvailableSeats = r.AvailableSeats,
                    DistanceKm = r.DistanceKm,
                    PricePerSeat = r.PricePerSeat,
                    Status = r.Status,
                    IsRecurring = r.IsRecurring
                })
                .ToListAsync();

            return Ok(rides);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRide([FromBody] RideCreateDto dto)
        {
            var driver = await _context.Driver.FindAsync(dto.DriverId);
            if (driver == null)
            {
                return NotFound("Driver not found");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId && v.DriverId == dto.DriverId);
            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            if (dto.DepartureTime < DateTime.UtcNow)
            {
                return BadRequest("Cannot create a ride in the past.");
            }

            var ride = new Ride
            {
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Origin = dto.Origin,
                Destination = dto.Destination,
                DepartureTime = dto.DepartureTime,
                TotalSeats = dto.TotalSeats,
                AvailableSeats = dto.TotalSeats,
                DistanceKm = dto.DistanceKm,
                PricePerSeat = 0,
                Status = "Scheduled",
                IsRecurring = dto.IsRecurring
            };

            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();

            if (dto.RoutePoints != null && dto.RoutePoints.Count > 0)
            {
                foreach (var rp in dto.RoutePoints)
                {
                    _context.RoutePoints.Add(new RoutePoint
                    {
                        RideId = ride.Id,
                        Location = rp.Location,
                        SequenceNumber = rp.SequenceNumber
                    });
                }
                await _context.SaveChangesAsync();
            }

            if (dto.IsRecurring && !string.IsNullOrEmpty(dto.RecurrencePattern) && dto.RecurrenceEndDate.HasValue)
            {
                var recurrence = new RideRecurrence
                {
                    RideId = ride.Id,
                    RecurrencePattern = dto.RecurrencePattern!,
                    EndDate = dto.RecurrenceEndDate.Value
                };
                _context.RideRecurrences.Add(recurrence);
                await _context.SaveChangesAsync();
            }

            var result = new RideDto
            {
                Id = ride.Id,
                DriverId = ride.DriverId,
                VehicleId = ride.VehicleId,
                Origin = ride.Origin,
                Destination = ride.Destination,
                DepartureTime = ride.DepartureTime,
                TotalSeats = ride.TotalSeats,
                AvailableSeats = ride.AvailableSeats,
                DistanceKm = ride.DistanceKm,
                PricePerSeat = ride.PricePerSeat,
                Status = ride.Status,
                IsRecurring = ride.IsRecurring
            };

            return CreatedAtAction(nameof(CreateRide), new { id = ride.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRide(int id)
        {
            var ride = await _context.Rides
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RideDto
                {
                    Id = r.Id,
                    DriverId = r.DriverId,
                    VehicleId = r.VehicleId,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DepartureTime = r.DepartureTime,
                    TotalSeats = r.TotalSeats,
                    AvailableSeats = r.AvailableSeats,
                    DistanceKm = r.DistanceKm,
                    PricePerSeat = r.PricePerSeat,
                    Status = r.Status,
                    IsRecurring = r.IsRecurring
                })
                .FirstOrDefaultAsync();

            if (ride == null)
            {
                return NotFound();
            }

            return Ok(ride);
        }

        [HttpGet("{id}/driver")]
        public async Task<IActionResult> GetRideDriver(int id)
        {
            var ride = await _context.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var driver = await _context.Driver
                .Include(d => d.DriverProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DriverId == ride.DriverId);

            if (driver == null || driver.DriverProfile == null)
            {
                return NotFound("Driver not found");
            }

            var profile = driver.DriverProfile;

            var driverDto = new DriverDto
            {
                Id = driver.DriverId,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                FullName = driver.FullName,
                LicenseNumber = profile.LicenseNumber,
                BackgroundCheckStatus = profile.BackgroundCheckStatus,
                DrivingExperienceYears = profile.DrivingExperienceYears,
                DOB = DateOnly.FromDateTime(profile.DOB)
            };

            return Ok(driverDto);
        }

        [HttpGet("{id}/vehicle")]
        public async Task<IActionResult> GetRideVehicle(int id)
        {
            var ride = await _context.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == ride.VehicleId);

            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            var vehicleDto = new VehicleDto
            {
                Id = vehicle.Id,
                CarMaker = vehicle.CarMaker,
                Model = vehicle.Model,
                VehicleType = vehicle.VehicleType,
                LicensePlate = vehicle.LicensePlate,
                Year = vehicle.Year,
                VerificationStatus = vehicle.VerificationStatus
            };

            return Ok(vehicleDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRide(int id, [FromBody] RideUpdateDto dto)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound();
            }

            ride.Origin = dto.Origin;
            ride.Destination = dto.Destination;
            ride.DepartureTime = dto.DepartureTime;
            ride.DistanceKm = dto.DistanceKm;
            ride.AvailableSeats += dto.TotalSeats - ride.TotalSeats;
            ride.TotalSeats = dto.TotalSeats;

            var existingRoutePoints = _context.RoutePoints.Where(rp => rp.RideId == id);
            _context.RoutePoints.RemoveRange(existingRoutePoints);

            if (dto.RoutePoints != null && dto.RoutePoints.Count > 0)
            {
                foreach (var rp in dto.RoutePoints)
                {
                    _context.RoutePoints.Add(new RoutePoint
                    {
                        RideId = id,
                        Location = rp.Location,
                        SequenceNumber = rp.SequenceNumber
                    });
                }
            }

            await _context.SaveChangesAsync();

            var result = new RideDto
            {
                Id = ride.Id,
                DriverId = ride.DriverId,
                VehicleId = ride.VehicleId,
                Origin = ride.Origin,
                Destination = ride.Destination,
                DepartureTime = ride.DepartureTime,
                TotalSeats = ride.TotalSeats,
                AvailableSeats = ride.AvailableSeats,
                DistanceKm = ride.DistanceKm,
                PricePerSeat = ride.PricePerSeat,
                Status = ride.Status,
                IsRecurring = ride.IsRecurring
            };

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRide(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            if (ride == null)
            {
                return NotFound();
            }

            var routePoints = _context.RoutePoints.Where(rp => rp.RideId == id);
            _context.RoutePoints.RemoveRange(routePoints);

            var recurrences = _context.RideRecurrences.Where(rr => rr.RideId == id);
            _context.RideRecurrences.RemoveRange(recurrences);

            _context.Rides.Remove(ride);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


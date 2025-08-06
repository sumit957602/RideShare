using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.RideManagement;
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
    }
}


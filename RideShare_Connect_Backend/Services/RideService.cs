using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.RideManagement;

namespace RideShare_Connect.Services
{
    public class RideService
    {
        private readonly ApplicationDbContext _db;
        private const decimal FuelPricePerLitre = 1.0m;
        private const decimal VehicleMileageKmPerLitre = 15m;

        public RideService(ApplicationDbContext db)
        {
            _db = db;
        }

        public decimal CalculatePrice(decimal distanceKm, int totalSeats)
        {
            var fuelCost = distanceKm / VehicleMileageKmPerLitre * FuelPricePerLitre;
            var totalCost = fuelCost * 1.1m; // 10% platform fee
            return Math.Round(totalCost / Math.Max(totalSeats, 1), 2);
        }

        public async Task<Ride> CreateRideAsync(RideCreateDto dto)
        {
            var ride = new Ride
            {
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Origin = dto.Origin,
                Destination = dto.Destination,
                DepartureTime = dto.DepartureTime,
                TotalSeats = dto.TotalSeats,
                AvailableSeats = dto.TotalSeats,
                Status = "Active",
                IsRecurring = dto.IsRecurring,
                PricePerSeat = CalculatePrice(dto.DistanceKm, dto.TotalSeats)
            };

            _db.Rides.Add(ride);
            await _db.SaveChangesAsync();

            foreach (var rp in dto.RoutePoints.OrderBy(p => p.SequenceNumber))
            {
                _db.RoutePoints.Add(new RoutePoint
                {
                    RideId = ride.Id,
                    Location = rp.Location,
                    SequenceNumber = rp.SequenceNumber
                });
            }

            if (dto.IsRecurring && !string.IsNullOrEmpty(dto.RecurrencePattern) && dto.RecurrenceEndDate.HasValue)
            {
                _db.RideRecurrences.Add(new RideRecurrence
                {
                    RideId = ride.Id,
                    RecurrencePattern = dto.RecurrencePattern!,
                    EndDate = dto.RecurrenceEndDate.Value
                });
            }

            await _db.SaveChangesAsync();
            return ride;
        }

        public async Task<Ride?> GetRideAsync(int id)
        {
            return await _db.Rides
                .Include(r => r.RoutePoints)
                .Include(r => r.Recurrences)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Ride?> EditRideAsync(int id, RideUpdateDto dto)
        {
            var ride = await _db.Rides.Include(r => r.RoutePoints)
                                      .FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null || ride.Status == "Cancelled") return null;

            ride.Origin = dto.Origin;
            ride.Destination = dto.Destination;
            ride.DepartureTime = dto.DepartureTime;
            ride.TotalSeats = dto.TotalSeats;
            ride.AvailableSeats = Math.Min(ride.AvailableSeats, dto.TotalSeats);
            ride.PricePerSeat = CalculatePrice(dto.DistanceKm, dto.TotalSeats);

            _db.RoutePoints.RemoveRange(ride.RoutePoints);
            foreach (var rp in dto.RoutePoints.OrderBy(p => p.SequenceNumber))
            {
                _db.RoutePoints.Add(new RoutePoint
                {
                    RideId = ride.Id,
                    Location = rp.Location,
                    SequenceNumber = rp.SequenceNumber
                });
            }

            await _db.SaveChangesAsync();
            return ride;
        }

        public async Task<bool> CancelRideAsync(int id)
        {
            var ride = await _db.Rides.FindAsync(id);
            if (ride == null) return false;
            if (ride.DepartureTime <= DateTime.UtcNow.AddHours(1))
            {
                return false; // Cancellation not allowed within 1 hour
            }

            ride.Status = "Cancelled";
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

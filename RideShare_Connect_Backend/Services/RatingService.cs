using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect_Backend.DTOs;
using System;
using Microsoft.EntityFrameworkCore;


namespace RideShare_Connect_Backend.Services
{
    public class RatingService: IRatingService
    {
        private readonly ApplicationDbContext _db;

        public RatingService(ApplicationDbContext db) => _db = db;

        public async Task<DriverRatingResponseDto> CreateAsync(int passengerId, DriverRatingCreateDto dto)
        {
            var ride = await _db.Rides
                .FirstOrDefaultAsync(r => r.Id == dto.RideId);

            if (ride == null)
                throw new InvalidOperationException("Ride not found.");

            if (ride.DriverId != dto.DriverId)
                throw new InvalidOperationException("Driver does not match the ride.");

            var hasCompletedBooking = await _db.RideBookings
                .AnyAsync(b => b.RideId == dto.RideId
                            && (b.Status == "Completed" ));

            if (!hasCompletedBooking)
                throw new InvalidOperationException("You can only rate rides you completed.");
            var reviewStr = "";
            if (dto.Rating == 1 && dto?.Review == null)
            {
                 reviewStr = "Very poor experience,The ride was uncomfortable";
            }else if(dto.Rating==2 && dto?.Review==null){
                 reviewStr = "Below average Service";
            }
            else if (dto.Rating == 3 && dto?.Review == null)
            {
                 reviewStr = "It was an okay ride.";
            }
            else if (dto.Rating == 4 && dto?.Review == null)
            {
                 reviewStr = "Good experience overall. The ride was comfortable";
            }
            else if (dto.Rating == 5 && dto?.Review == null)
            {
                 reviewStr = "Excellent service! Smooth ride, punctual and very comfortable";
            }
            if (dto?.Review == null)
            {
                dto.Review = reviewStr;
            }


            var entity = new DriverRating
            {
                RideId = dto.RideId,
                DriverId = dto.DriverId,
                PassengerId = passengerId,
                Rating = dto.Rating,
                Review = dto.Review?.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _db.DriverRatings.Add(entity);
            await _db.SaveChangesAsync();

            return new DriverRatingResponseDto
            {
                Id = entity.Id,
                RideId = entity.RideId,
                DriverId = entity.DriverId,
                PassengerId = entity.PassengerId,
                Rating = entity.Rating,
                Review = entity.Review,
                Timestamp = entity.Timestamp
            };
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.RideManagement;
using System.Data;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RideBookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RideBookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("accept")]
        public async Task<IActionResult> AcceptRide([FromBody] RideBookingRequestDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == dto.RideId);
            if (ride == null)
                return NotFound("Ride not found");

            if (ride.Status != "Scheduled")
                return BadRequest("ride closed");

            if (ride.DepartureTime <= DateTime.UtcNow)
                return BadRequest("ride past");

            if (ride.AvailableSeats < dto.Seats)
                return BadRequest("not enough seats");

            ride.AvailableSeats -= dto.Seats;

            var booking = new RideBooking
            {
                RideId = dto.RideId,
                PassengerId = dto.UserId,
                BookedSeats = dto.Seats,
                PickupLocation = dto.PickupLocation,
                DropLocation = dto.DropLocation,
                DistanceKm = dto.DistanceKm,
                BookingTime = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.RideBookings.Add(booking);

            var payment = new Payment
            {
                UserId = dto.UserId,
                Booking = booking,
                Amount = ride.PricePerSeat * dto.Seats,
                PaymentDate = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { bookingId = booking.Id, paymentId = payment.Id, paymentStatus = payment.Status });
        }
    }
}

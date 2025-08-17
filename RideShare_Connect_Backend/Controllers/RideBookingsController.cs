using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.RideManagement;
using System.Data;
using System;
using System.Linq;

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

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetBookingsByDriver(int driverId)
        {
            var rideIds = await _context.Rides
                .Where(r => r.DriverId == driverId)
                .Select(r => r.Id)
                .ToListAsync();

            if (!rideIds.Any())
            {
                return NotFound("No rides found for the driver");
            }

            var bookings = await _context.RideBookings
                .Include(rb => rb.Ride)
                .Where(rb => rideIds.Contains(rb.RideId))
                .Select(rb => new RideBookingDetailsDto
                {
                    BookingId = rb.Id,
                    RideId = rb.RideId,
                    PassengerId = rb.PassengerId,
                    BookedSeats = rb.BookedSeats,
                    PickupLocation = rb.PickupLocation,
                    DropLocation = rb.DropLocation,
                    DistanceKm = rb.DistanceKm,
                    BookingTime = rb.BookingTime,
                    Status = rb.Status,
                    Origin = rb.Ride.Origin,
                    Destination = rb.Ride.Destination,
                    DepartureTime = rb.Ride.DepartureTime,
                    PricePerSeat = rb.Ride.PricePerSeat
                })
                .ToListAsync();

            if (!bookings.Any())
            {
                return NotFound("No bookings found for the driver");
            }

            return Ok(bookings);
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

            if (ride.AvailableSeats < dto.NumPersons)
                return BadRequest("not enough seats");

            if (!new[] { "Wallet", "Razor Pay", "Cash" }.Contains(dto.PaymentMode))
                return BadRequest("invalid payment mode");

            ride.AvailableSeats -= dto.NumPersons;

            var booking = new RideBooking
            {
                RideId = dto.RideId,
                PassengerId = dto.UserId,
                BookedSeats = dto.NumPersons,
                PickupLocation = dto.PickupLocation,
                DropLocation = dto.DropLocation,
                DistanceKm = dto.DistanceKm,
                BookingTime = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.RideBookings.Add(booking);

            var totalFare = ride.PricePerSeat * dto.NumPersons * dto.DistanceKm;

            Wallet? wallet = null;
            if (dto.PaymentMode == "Wallet")
            {
                wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == dto.UserId);
                if (wallet == null || wallet.Balance < totalFare)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("You have not sufficient balance in your wallet");
                }

                wallet.Balance -= totalFare;
                wallet.LastUpdated = DateTime.UtcNow;
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    Wallet = wallet,
                    Amount = totalFare,
                    TxnType = "Debit",
                    TxnDate = DateTime.UtcNow,
                    Description = "Ride booking",
                    TransactionId = Guid.NewGuid().ToString(),
                    PaymentMethod = "Wallet",
                    Status = "Completed"
                });
            }

            var payment = new Payment
            {
                UserId = dto.UserId,
                Booking = booking,
                Amount = totalFare,
                PaymentMode = dto.PaymentMode,
                PaymentDate = DateTime.UtcNow,
                Status = dto.PaymentMode == "Wallet" ? "Completed" : "Pending"
            };
            _context.Payments.Add(payment);

            var transactionSummary = await _context.UserTransactionSummaries
                .OrderByDescending(t => t.TransactionId)
                .FirstOrDefaultAsync(t => t.UserId == dto.UserId);

            if (transactionSummary == null)
            {
                transactionSummary = new UserTransactionSummary
                {
                    RideId = ride.Id,
                    DriverId = ride.DriverId,
                    UserId = dto.UserId,
                    TotalAmount = totalFare,
                    TotalTransactionAmount = dto.PaymentMode == "Cash" ? 0 : totalFare,
                };
                _context.UserTransactionSummaries.Add(transactionSummary);
            }
            else
            {
                transactionSummary.RideId = ride.Id;
                transactionSummary.DriverId = ride.DriverId;
                transactionSummary.TotalAmount += totalFare;
                if (dto.PaymentMode != "Cash")
                    transactionSummary.TotalTransactionAmount += totalFare;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { bookingId = booking.Id, paymentId = payment.Id, paymentStatus = payment.Status, totalFare });
        }
    }
}

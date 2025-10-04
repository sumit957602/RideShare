using Microsoft.EntityFrameworkCore;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;
using RideShare_Connect.Models.RideManagement;
using RideShareConnect.Data;
using System.Data;
using System.Linq;

namespace RideShare_Connect.Services
{
    public class RideBookingService : IRideBookingService
    {
        private readonly AppDbContext _context;

        public RideBookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RideBookingResultDto> BookRideAsync(RideBookingRequestDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == dto.RideId);
            if (ride == null)
            {
                throw new ServiceException("Ride not found.");
            }

            if (!string.Equals(ride.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                throw new ServiceException("Ride is not available for booking.");
            }

            if (ride.DepartureTime <= DateTime.UtcNow)
            {
                throw new ServiceException("Ride departure time has passed.");
            }

            if (ride.AvailableSeats < dto.NumPersons)
            {
                throw new ServiceException("Not enough seats available.");
            }

            if (!new[] { "Wallet", "Razor Pay", "Cash" }.Contains(dto.PaymentMode))
            {
                throw new ServiceException("Invalid payment mode.");
            }

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
                    throw new ServiceException("Insufficient wallet balance.");
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
                {
                    transactionSummary.TotalTransactionAmount += totalFare;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RideBookingResultDto
            {
                BookingId = booking.Id,
                PaymentId = payment.Id,
                PaymentStatus = payment.Status,
                TotalFare = totalFare
            };
        }
    }
}

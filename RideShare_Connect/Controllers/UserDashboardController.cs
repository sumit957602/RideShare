using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.ViewModels;
using RideShareConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using RideShare_Connect.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IUserAccountService _userAccountService;
        private readonly IRideBookingService _rideBookingService;
        private readonly ILogger<UserDashboardController> _logger;
        private readonly IConfiguration _config;

        public UserDashboardController(AppDbContext db, IUserAccountService userAccountService, IRideBookingService rideBookingService, ILogger<UserDashboardController> logger, IConfiguration config)
        {
            _db = db;
            _userAccountService = userAccountService;
            _rideBookingService = rideBookingService;
            _logger = logger;
            _config = config;
        }

        public IActionResult User()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var profile = _db.UserProfiles.FirstOrDefault(u => u.UserId == userId);

            var bookingsQuery = _db.RideBookings
                .Include(rb => rb.Ride)
                    .ThenInclude(r => r.Driver)
                .Include(rb => rb.Ride)
                    .ThenInclude(r => r.Vehicle)
                .Where(rb => rb.PassengerId == userId);

            var bookings = bookingsQuery
                .OrderByDescending(rb => rb.BookingTime)
                .ToList();

            var totalSpent = bookings.Sum(rb => rb.BookedSeats * rb.Ride.PricePerSeat);

            var wallet = _db.Wallets.FirstOrDefault(w => w.UserId == userId);
            var walletBalance = wallet?.Balance ?? 0;

            var rating = _db.DriverRatings
                .Where(r => r.DriverId == userId)
                .Average(r => (double?)r.Rating) ?? 0;

            var availableRides = _db.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .Where(r => r.AvailableSeats > 0 && r.DepartureTime >= DateTime.Now)
                .OrderBy(r => r.DepartureTime)
                .ToList();

            var bookingIds = bookings.Select(b => b.Id).ToList();
            var paymentModes = _db.Payments
                .Include(p => p.PaymentMethod)
                .Where(p => bookingIds.Contains(p.BookingId))
                .OrderByDescending(p => p.PaymentDate)
                .AsEnumerable()
                .GroupBy(p => p.BookingId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var payment = g.FirstOrDefault();
                        return payment?.PaymentMethod?.CardType ?? "Cash";
                    });

            var activeRidesCount = bookings.Count(b => b.Status == "Ongoing");
            var cancelledRidesCount = bookings.Count(b => b.Status == "Cancelled");
            var completedRidesCount = bookings.Count(b => b.Status == "Completed");
            var totalBookingsCount = bookings.Count;

            var model = new UserDashboardViewModel
            {
                Profile = profile,
                RidesBooked = bookings.Count,
                TotalSpent = totalSpent,
                WalletBalance = walletBalance,
                Rating = rating,
                Bookings = bookings,
                RecentBookings = bookings.Take(5).ToList(),
                AvailableRides = availableRides,
                PaymentModes = paymentModes,
                ActiveRidesCount = activeRidesCount,
                CancelledRidesCount = cancelledRidesCount,
                CompletedRidesCount = completedRidesCount,
                TotalBookingsCount = totalBookingsCount
            };

            return View(model);
        }

        public async Task<IActionResult> UserProfile()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            try
            {
                var userProfile = await _userAccountService.GetProfileAsync(userId);
                return View(userProfile);
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Failed to fetch profile for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", userId);
            }

            return View(new UserProfileDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserProfile(UserProfileDto profile)
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var updateDto = new UserProfileUpdateDto
            {
                FullName = profile.FullName,
                ProfilePicture = profile.ProfilePicture,
                EmergencyContact = profile.EmergencyContact,
                Bio = profile.Bio,
                PhoneNumber = profile.PhoneNumber
            };

            try
            {
                await _userAccountService.UpdateProfileAsync(userId, updateDto);
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating profile for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating profile for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "An error occurred while updating profile.");
                return View(profile);
            }

            return RedirectToAction(nameof(UserProfile));
        }

        public IActionResult PrivacySettings()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var profile = _db.UserProfiles.FirstOrDefault(u => u.UserId == userId);
            return View(profile);
        }

        public IActionResult DeleteAccount()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "UserAccount");
            }
            return View(new DeleteAccountDto());
        }

        public async Task<IActionResult> RideDetails(int id)
        {
            var ride = await _db.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound();
            }

            var riders = await _db.RideBookings
                .Include(rb => rb.User)
                .ThenInclude(u => u.UserProfile)
                .Where(rb => rb.RideId == id)
                .Select(rb => rb.User.UserProfile != null ? rb.User.UserProfile.FullName : rb.User.Email)
                .ToListAsync();

            var viewModel = new RideDetailsViewModel
            {
                Ride = ride,
                Riders = riders
            };

            return View(viewModel);
        }

        public async Task<IActionResult> BookRide(int id)
        {
            var ride = await _db.Rides
                .Include(r => r.Driver)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound();
            }

            ViewBag.RazorpayKey = _config["PaymentGateway:ApiKey"] ?? "rzp_test_R5NMyugFaCywDS";
            return View(ride);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookRide(int id, [FromBody] RideBookingRequestDto dto)
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var requestDto = new RideBookingRequestDto
            {
                RideId = id,
                UserId = userId,
                NumPersons = dto.NumPersons,
                PaymentMode = dto.PaymentMode,
                PickupLocation = dto.PickupLocation,
                DropLocation = dto.DropLocation,
                DistanceKm = dto.DistanceKm
            };

            try
            {
                var result = await _rideBookingService.BookRideAsync(requestDto);
                return Json(result);
            }
            catch (ServiceException ex)
            {
                _logger.LogWarning(ex, "Failed to book ride {RideId} for user {UserId}", id, userId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while booking ride {RideId} for user {UserId}", id, userId);
                return StatusCode(500, "An unexpected error occurred while booking the ride.");
            }
        }
    }
}

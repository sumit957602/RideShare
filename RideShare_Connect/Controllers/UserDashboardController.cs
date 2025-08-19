using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.ViewModels;
using RideShareConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using RideShare_Connect.DTOs;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace RideShare_Connect.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserDashboardController> _logger;
        private readonly IConfiguration _config;

        public UserDashboardController(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<UserDashboardController> logger, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
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
                .Where(r => r.AvailableSeats > 0 && r.DepartureTime >= DateTime.Now && r.Status!="Completed")
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.GetAsync($"api/Users/profile?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var userProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                    return View(userProfile);
                }

                _logger.LogError("Failed to fetch profile for user {UserId}: {StatusCode}", userId, response.StatusCode);
            }
            catch (HttpRequestException ex)
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/Users/profile?userId={userId}", updateDto);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error updating profile for user {UserId}: {StatusCode} - {Error}", userId, response.StatusCode, errorContent);

                    try
                    {
                        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(errorContent);
                        if (problemDetails?.Errors != null)
                        {
                            foreach (var key in problemDetails.Errors.Keys)
                            {
                                foreach (var error in problemDetails.Errors[key])
                                {
                                    ModelState.AddModelError(key, error);
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, errorContent);
                        }
                    }
                    catch (JsonException)
                    {
                        ModelState.AddModelError(string.Empty, errorContent);
                    }

                    return View(profile);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error while updating profile for user {UserId}", userId);
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

            var httpClient = _httpClientFactory.CreateClient("RideShareApi");
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

            var response = await httpClient.PostAsJsonAsync("api/RideBookings/accept", requestDto);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return Content(content, "application/json");
            }

            return StatusCode((int)response.StatusCode, content);
        }
    }
}

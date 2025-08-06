using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.ViewModels;
using RideShareConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using RideShare_Connect.DTOs;
using System.Threading.Tasks;

namespace RideShare_Connect.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _db;

        public UserDashboardController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> User()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(u => u.UserId == userId);

            var bookingsQuery = _db.RideBookings
                .Include(rb => rb.Ride)
                    .ThenInclude(r => r.Driver)
                .Where(rb => rb.PassengerId == userId);

            var bookings = await bookingsQuery
                .OrderByDescending(rb => rb.BookingTime)
                .ToListAsync();

            var totalSpent = bookings.Sum(rb => rb.BookedSeats * rb.Ride.PricePerSeat);

            var rating = await _db.DriverRatings
                .Where(r => r.DriverId == userId)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            var availableRides = await _db.Rides
                .AsNoTracking()
                .Include(r => r.Driver)
                .Where(r => r.Status == "Scheduled" && r.AvailableSeats > 0 && r.DepartureTime >= DateTime.UtcNow)
                .OrderBy(r => r.DepartureTime)
                .ToListAsync();

            var model = new UserDashboardViewModel
            {
                Profile = profile,
                RidesBooked = bookings.Count,
                TotalSpent = totalSpent,
                Rating = rating,
                Bookings = bookings,
                RecentBookings = bookings.Take(5).ToList(),
                AvailableRides = availableRides
            };

            return View(model);
        }

        public IActionResult UserProfile()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var userProfile = _db.UserProfiles.FirstOrDefault(u => u.UserId == userId);
            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UserProfile(UserProfile profile)
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "UserAccount");
            }

            var existingProfile = _db.UserProfiles.FirstOrDefault(u => u.UserId == userId);
            if (existingProfile == null)
            {
                profile.UserId = userId;
                _db.UserProfiles.Add(profile);
            }
            else
            {
                existingProfile.FullName = profile.FullName;
                existingProfile.ProfilePicture = profile.ProfilePicture;
                existingProfile.EmergencyContact = profile.EmergencyContact;
                existingProfile.Bio = profile.Bio;
                _db.UserProfiles.Update(existingProfile);
            }

            _db.SaveChanges();

            return RedirectToAction(nameof(UserProfile));
        }

        public IActionResult PrivacySettings()
        {
            return View();
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
    }
}

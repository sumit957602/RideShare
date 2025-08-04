using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.ViewModels;
using System.Security.Claims;

namespace RideShare_Connect.Controllers
{
    public class DriverDashboardController : Controller
    {
        private readonly AppDbContext _db;

        public DriverDashboardController(AppDbContext db)
        {
            _db = db;
        }

        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> Driver()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
            var ratings = await _db.DriverRatings.Where(r => r.DriverId == userId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
            var average = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;

            var viewModel = new DriverDashboardViewModel
            {
                Profile = profile,
                Vehicles = vehicles,
                AverageRating = average,
                RecentRatings = ratings.Take(5)
            };

            return View(viewModel);
        }
    }
}

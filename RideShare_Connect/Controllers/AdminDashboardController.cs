using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.ViewModels;
using System.Security.Claims;

namespace RideShare_Connect.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public AdminDashboardController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Admin()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalRevenue = _db.Payments.Where(p => p.Status == "Completed").Sum(p => (decimal?)p.Amount) ?? 0,
                TotalUsers = _db.Users.Count(),
                RidesCompleted = _db.Rides.Count(r => r.Status == "Completed"),
                OpenReports = _db.UserReports.Count(r => r.Status == "Pending"),
                Users = _db.Users.Include(u => u.UserProfile).ToList(),
                Drivers = _db.Set<Driver>().ToList(),
                Vehicles = _db.Vehicles.Include(v => v.Driver).ToList()
            };

            return View(viewModel);
        }
    }
}

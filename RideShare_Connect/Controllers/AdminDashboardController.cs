using Microsoft.AspNetCore.Mvc;
using RideShareConnect.Data;
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

            return View();
        }
    }
}

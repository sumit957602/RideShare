using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.Models;
using RideShareConnect.Data;
using System.Diagnostics;

namespace RideShare_Connect.Controllers
{
    public class DriverDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public DriverDashboardController(AppDbContext db)
        {
            _db = db;
        }
        public IActionResult Driver()
        {
            return View();
        }
    }
}

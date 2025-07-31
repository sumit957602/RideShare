using Microsoft.AspNetCore.Mvc;
using RideShareConnect.Data;

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
            return View();
        }
    }
}

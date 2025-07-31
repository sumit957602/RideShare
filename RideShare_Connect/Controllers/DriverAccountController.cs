using Microsoft.AspNetCore.Mvc;
using RideShareConnect.Data;

namespace RideShare_Connect.Controllers
{
    public class DriverAccountController : Controller
    {
        private readonly AppDbContext _db;
        public DriverAccountController(AppDbContext db)
        {
            _db = db;
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
    }
}

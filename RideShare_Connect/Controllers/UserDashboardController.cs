using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.Models.UserManagement;
using RideShareConnect.Data;
using System.Linq;

namespace RideShare_Connect.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly AppDbContext _db;

        public UserDashboardController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult User()
        {
            var userProfile = _db.UserProfiles.FirstOrDefault(u => u.Id == 2);

            return View(userProfile);
        }

        
        public IActionResult UserProfile()
        {
            var userProfile = _db.UserProfiles.FirstOrDefault(u => u.Id == 2);
            return View(userProfile);
        }

        public IActionResult PrivacySettings()
        {
            return View();
        }
    }
}

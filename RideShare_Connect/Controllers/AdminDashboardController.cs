using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

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

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            var user = await _db.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                UserType = user.UserType,
                AccountStatus = user.AccountStatus
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _db.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserType = model.UserType;
            user.AccountStatus = model.AccountStatus;

            if (user.UserProfile != null)
            {
                user.UserProfile.FullName = model.FullName;
            }
            else if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                user.UserProfile = new RideShare_Connect.Models.UserManagement.UserProfile
                {
                    FullName = model.FullName,
                    UserId = user.Id
                };
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            var user = await _db.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.UserProfile != null)
            {
                _db.UserProfiles.Remove(user.UserProfile);
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return RedirectToAction("Admin");
        }

        [HttpGet]
        public async Task<IActionResult> EditDriver(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            var driver = await _db.Set<Driver>().Include(d => d.DriverProfile)
                                .FirstOrDefaultAsync(d => d.DriverId == id);
            if (driver == null)
            {
                return NotFound();
            }

            var model = new EditDriverViewModel
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                LicenseNumber = driver.DriverProfile?.LicenseNumber,
                BackgroundCheckStatus = driver.DriverProfile?.BackgroundCheckStatus,
                DrivingExperienceYears = driver.DriverProfile?.DrivingExperienceYears ?? 0,
                ReferenceContacts = driver.DriverProfile?.ReferenceContacts
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDriver(EditDriverViewModel model)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var driver = await _db.Set<Driver>().Include(d => d.DriverProfile)
                                .FirstOrDefaultAsync(d => d.DriverId == model.DriverId);
            if (driver == null)
            {
                return NotFound();
            }

            driver.FullName = model.FullName;
            driver.Email = model.Email;
            driver.PhoneNumber = model.PhoneNumber;

            if (driver.DriverProfile != null)
            {
                driver.DriverProfile.LicenseNumber = model.LicenseNumber;
                driver.DriverProfile.BackgroundCheckStatus = model.BackgroundCheckStatus;
                driver.DriverProfile.DrivingExperienceYears = model.DrivingExperienceYears;
                driver.DriverProfile.ReferenceContacts = model.ReferenceContacts;
            }
            else if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
            {
                driver.DriverProfile = new DriverProfile
                {
                    DriverId = driver.DriverId,
                    LicenseNumber = model.LicenseNumber,
                    BackgroundCheckStatus = model.BackgroundCheckStatus,
                    DrivingExperienceYears = model.DrivingExperienceYears,
                    ReferenceContacts = model.ReferenceContacts
                };
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (!User.Identity.IsAuthenticated || role != "Admin")
            {
                return RedirectToAction("AdminLogin", "AdminAccount");
            }

            var driver = await _db.Set<Driver>().Include(d => d.DriverProfile)
                                 .FirstOrDefaultAsync(d => d.DriverId == id);
            if (driver == null)
            {
                return NotFound();
            }

            var vehicles = await _db.Vehicles.Where(v => v.DriverId == id).ToListAsync();
            if (vehicles.Any())
            {
                _db.Vehicles.RemoveRange(vehicles);
            }

            if (driver.DriverProfile != null)
            {
                _db.DriverProfiles.Remove(driver.DriverProfile);
            }

            _db.Set<Driver>().Remove(driver);
            await _db.SaveChangesAsync();

            return RedirectToAction("Admin");
        }
    }
}

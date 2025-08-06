using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.VehicleManagement;
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

            var driver = await _db.Set<Driver>()
                .Include(d => d.DriverProfile)
                .FirstOrDefaultAsync(d => d.DriverId == userId);
            var driverProfile = driver?.DriverProfile;
            var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
            var ratings = await _db.DriverRatings.Where(r => r.DriverId == userId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
            var average = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;

            var viewModel = new DriverDashboardViewModel
            {
                Driver = driver,
                DriverProfile = driverProfile,
                Vehicles = vehicles,
                AverageRating = average,
                RecentRatings = ratings.Take(5),
                NewVehicle = new AddVehicleViewModel()
            };

            return View(viewModel);
        }
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> CreateRide()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();

            var viewModel = new CreateRideViewModel
            {
                Vehicles = vehicles,
                DepartureDate = DateTime.Today
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRide(CreateRideViewModel model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            if (!ModelState.IsValid)
            {
                model.Vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
                return View(model);
            }

            var departure = model.DepartureDate.Date + model.DepartureTime;

            var ride = new Ride
            {
                DriverId = userId,
                VehicleId = model.VehicleId,
                Origin = model.Origin,
                Destination = model.Destination,
                DepartureTime = departure,
                TotalSeats = model.Seats,
                AvailableSeats = model.Seats,
                PricePerSeat = model.PricePerSeat,
                DistanceKm = model.DistanceKm,
                Status = "Scheduled",
                IsRecurring = model.IsRecurring
            };

            _db.Rides.Add(ride);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVehicle(AddVehicleViewModel model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            if (!ModelState.IsValid)
            {
                var driver = await _db.Set<Driver>()
                    .Include(d => d.DriverProfile)
                    .FirstOrDefaultAsync(d => d.DriverId == userId);
                var driverProfile = driver?.DriverProfile;
                var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
                var ratings = await _db.DriverRatings.Where(r => r.DriverId == userId)
                    .OrderByDescending(r => r.Timestamp)
                    .ToListAsync();
                var average = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;

                var viewModel = new DriverDashboardViewModel
                {
                    Driver = driver,
                    DriverProfile = driverProfile,
                    Vehicles = vehicles,
                    AverageRating = average,
                    RecentRatings = ratings.Take(5),
                    NewVehicle = model
                };

                return View("Driver", viewModel);
            }

            var vehicle = new Vehicle
            {
                DriverId = userId,
                CarMaker = model.CarMaker,
                Model = model.Model,
                VehicleType = model.VehicleType,
                LicensePlate = model.LicensePlate,
                Status = "Pending",
                Year = model.Year
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> EditProfile()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var driver = await _db.Set<Driver>()
                .Include(d => d.DriverProfile)
                .FirstOrDefaultAsync(d => d.DriverId == userId);
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

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditDriverViewModel model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            if (userId != model.DriverId)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var driver = await _db.Set<Driver>()
                .Include(d => d.DriverProfile)
                .FirstOrDefaultAsync(d => d.DriverId == userId);
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
            else
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

            return RedirectToAction(nameof(Driver));
        }
    }
}

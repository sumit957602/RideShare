using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.ViewModels;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.PaymentManagement;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace RideShare_Connect.Controllers
{
    public class DriverDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DriverDashboardController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public DriverDashboardController(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<DriverDashboardController> logger)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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

            var rideIds = await _db.Rides
                .Where(r => r.DriverId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            var rides = new List<Ride>();
            List<RideBookingDetailsDto> acceptedBookings = new List<RideBookingDetailsDto>();

            if (rideIds.Any())
            {
                rides = await _db.Rides
                    .Where(r => rideIds.Contains(r.Id))
                    .Include(r => r.Vehicle)
                    .OrderByDescending(r => r.DepartureTime)
                    .ToListAsync();

                acceptedBookings = await _db.RideBookings
                    .Include(rb => rb.Ride)
                    .Where(rb => rideIds.Contains(rb.RideId))
                    .Select(rb => new RideBookingDetailsDto
                    {
                        BookingId = rb.Id,
                        RideId = rb.RideId,
                        PassengerId = rb.PassengerId,
                        BookedSeats = rb.BookedSeats,
                        PickupLocation = rb.PickupLocation,
                        DropLocation = rb.DropLocation,
                        DistanceKm = rb.DistanceKm,
                        BookingTime = rb.BookingTime,
                        Status = rb.Status,
                        Origin = rb.Ride.Origin,
                        Destination = rb.Ride.Destination,
                        DepartureTime = rb.Ride.DepartureTime,
                        PricePerSeat = rb.Ride.PricePerSeat
                    })
                    .ToListAsync();
            }
            var average = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0;

            var wallet = await _db.DriverWallets.FirstOrDefaultAsync(w => w.DriverId == userId);
            var walletBalance = wallet?.Balance ?? 0m;
            decimal totalEarnings = 0m;
            if (wallet != null)
            {
                totalEarnings = await _db.DriverWalletTransactions
                    .Where(t => t.DriverWalletId == wallet.Id &&
                                t.TxnType.ToLower() == "credit" &&
                                t.Status.ToLower() == "successful")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;
            }

            // Calculate overview statistics
            var vehicleCount = vehicles.Count;
            var createdRidesCount = rides.Count;
            var acceptedRidesCount = acceptedBookings.Count(b => b.Status == "Pending");
            var activeRidesCount = acceptedBookings.Count(b => b.Status == "Ongoing");
            var cancelledRidesCount = acceptedBookings.Count(b => b.Status == "Cancelled");
            var totalRidesCount = acceptedBookings.Count;

            var viewModel = new DriverDashboardViewModel
            {
                Driver = driver,
                DriverProfile = driverProfile,
                Vehicles = vehicles,
                CreatedRides = rides,
                AcceptedRideBookings = acceptedBookings,
                AverageRating = average,
                RecentRatings = ratings.Take(5),
                WalletBalance = walletBalance,
                TotalEarnings = totalEarnings,
                VehicleCount = vehicleCount,
                CreatedRidesCount = createdRidesCount,
                AcceptedRidesCount = acceptedRidesCount,
                ActiveRidesCount = activeRidesCount,
                CancelledRidesCount = cancelledRidesCount,
                TotalRidesCount = totalRidesCount
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

            if (departure < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Departure time cannot be in the past.");
                model.Vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
                return View(model);
            }

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
        public async Task<IActionResult> EditRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == id && r.DriverId == userId);
            if (ride == null)
            {
                return NotFound();
            }

            var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();

            var model = new EditRideViewModel
            {
                Id = ride.Id,
                Origin = ride.Origin,
                Destination = ride.Destination,
                DepartureDate = ride.DepartureTime.Date,
                DepartureTime = ride.DepartureTime.TimeOfDay,
                Seats = ride.TotalSeats,
                PricePerSeat = ride.PricePerSeat,
                VehicleId = ride.VehicleId,
                DistanceKm = ride.DistanceKm,
                IsRecurring = ride.IsRecurring,
                Vehicles = vehicles
            };

            return View(model);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRide(EditRideViewModel model)
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

            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == model.Id && r.DriverId == userId);
            if (ride == null)
            {
                return NotFound();
            }

            var departure = model.DepartureDate.Date + model.DepartureTime;
            if (departure < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Departure time cannot be in the past.");
                model.Vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();
                return View(model);
            }

            var bookedSeats = ride.TotalSeats - ride.AvailableSeats;
            ride.Origin = model.Origin;
            ride.Destination = model.Destination;
            ride.DepartureTime = departure;
            ride.VehicleId = model.VehicleId;
            ride.TotalSeats = model.Seats;
            ride.AvailableSeats = Math.Max(0, model.Seats - bookedSeats);
            ride.PricePerSeat = model.PricePerSeat;
            ride.DistanceKm = model.DistanceKm;
            ride.IsRecurring = model.IsRecurring;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> ViewRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == id && r.DriverId == userId);
            if (ride == null)
            {
                return NotFound();
            }

            var vehicles = await _db.Vehicles.Where(v => v.DriverId == userId).ToListAsync();

            var model = new EditRideViewModel
            {
                Id = ride.Id,
                Origin = ride.Origin,
                Destination = ride.Destination,
                DepartureDate = ride.DepartureTime.Date,
                DepartureTime = ride.DepartureTime.TimeOfDay,
                Seats = ride.TotalSeats,
                PricePerSeat = ride.PricePerSeat,
                VehicleId = ride.VehicleId,
                DistanceKm = ride.DistanceKm,
                IsRecurring = ride.IsRecurring,
                Vehicles = vehicles
            };

            return View(model);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == id && r.DriverId == userId);
            if (ride == null)
            {
                return NotFound();
            }

            _db.Rides.Remove(ride);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var booking = await _db.RideBookings
                .Include(rb => rb.Ride)
                .FirstOrDefaultAsync(rb => rb.Id == id && rb.Ride.DriverId == userId);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Ongoing";
            booking.Ride.Status = "Ongoing";

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpGet]
        public async Task<IActionResult> CompleteRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var booking = await _db.RideBookings
                .Include(rb => rb.Ride)
                .FirstOrDefaultAsync(rb => rb.Id == id && rb.Ride.DriverId == userId);
            if (booking == null || booking.Status != "Ongoing")
            {
                return NotFound();
            }

            var dto = new RideBookingDetailsDto
            {
                BookingId = booking.Id,
                RideId = booking.RideId,
                PassengerId = booking.PassengerId,
                BookedSeats = booking.BookedSeats,
                PickupLocation = booking.PickupLocation,
                DropLocation = booking.DropLocation,
                DistanceKm = booking.DistanceKm,
                BookingTime = booking.BookingTime,
                Status = booking.Status,
                Origin = booking.Ride.Origin,
                Destination = booking.Ride.Destination,
                DepartureTime = booking.Ride.DepartureTime,
                PricePerSeat = booking.Ride.PricePerSeat
            };

            return View(dto);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRide(int id, bool confirm)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            if (!confirm)
            {
                return RedirectToAction(nameof(Driver));
            }

            var booking = await _db.RideBookings
                .Include(rb => rb.Ride)
                .FirstOrDefaultAsync(rb => rb.Id == id && rb.Ride.DriverId == userId);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Completed";
            booking.Ride.Status = "Completed";

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRide(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var booking = await _db.RideBookings
                .Include(rb => rb.Ride)
                .FirstOrDefaultAsync(rb => rb.Id == id && rb.Ride.DriverId == userId);
            if (booking == null)
            {
                return NotFound();
            }

            booking.Status = "Cancelled";
            booking.Ride.AvailableSeats += booking.BookedSeats;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpGet]
        public IActionResult AddVehicle()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }
            return View();
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterVehicle(VehicleRegisterDto model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }
            if (!ModelState.IsValid)
            {
                return RedirectToAction("AddVehicle", "DriverDashboard");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient("RideShareApi");
                var dto = new VehicleRegisterDto
                {
                    CarMaker = model.CarMaker,
                    CarModel = model.CarModel,
                    VehicleType = model.VehicleType,
                    LicensePlate = model.LicensePlate,
                    Year = model.Year
                };

                var response = await httpClient.PostAsJsonAsync($"api/Vehicles/register?driverId={userId}", dto);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Driver));
                }

                string? apiError;
                try
                {
                    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                    apiError = problem?.Detail ?? problem?.Title;
                }
                catch (Exception)
                {
                    apiError = await response.Content.ReadAsStringAsync();
                }

                _logger.LogError("Failed to register vehicle: {Error}", apiError);
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(apiError) ? "Failed to register vehicle." : apiError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering vehicle");
                ModelState.AddModelError(string.Empty, "An error occurred while registering vehicle.");
            }

            return RedirectToAction("AddVehicle", "DriverDashboard");
        }

        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> EditVehicle(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.DriverId == userId);
            if (vehicle == null)
            {
                return NotFound();
            }

            var model = new EditVehicleViewModel
            {
                Id = vehicle.Id,
                CarMaker = vehicle.CarMaker,
                Model = vehicle.Model,
                VehicleType = vehicle.VehicleType,
                LicensePlate = vehicle.LicensePlate,
                VerificationStatus = vehicle.VerificationStatus,
                Year = vehicle.Year
            };

            return View(model);
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVehicle(EditVehicleViewModel model)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == model.Id && v.DriverId == userId);
            if (vehicle == null)
            {
                return NotFound();
            }

            vehicle.CarMaker = model.CarMaker;
            vehicle.Model = model.Model;
            vehicle.VehicleType = model.VehicleType;
            vehicle.LicensePlate = model.LicensePlate;
            vehicle.Year = model.Year;
            vehicle.VerificationStatus = model.VerificationStatus;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }

        [Authorize(Roles = "Driver")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "DriverAccount");
            }

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.DriverId == userId);
            if (vehicle == null)
            {
                return NotFound();
            }

            _db.Vehicles.Remove(vehicle);
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
                DOB = driver.DriverProfile?.DOB ?? DateTime.MinValue
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
                driver.DriverProfile.DOB = model.DOB;
            }
            else
            {
                driver.DriverProfile = new DriverProfile
                {
                    DriverId = driver.DriverId,
                    LicenseNumber = model.LicenseNumber,
                    BackgroundCheckStatus = model.BackgroundCheckStatus,
                    DrivingExperienceYears = model.DrivingExperienceYears,
                    DOB = model.DOB
                };
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Driver));
        }
    }
}

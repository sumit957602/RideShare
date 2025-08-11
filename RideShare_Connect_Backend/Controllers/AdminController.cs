using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.AdminManagement;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect.Models.RideManagement;
using RideShare_Connect.DTOs;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Admin> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IPasswordHasher<Admin> passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.Admins
                .Select(a => new AdminDto
                {
                    Id = a.Id,
                    Username = a.Username
                })
                .ToListAsync();

            return Ok(admins);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin(int id, AdminUpdateDto dto)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.Username)) admin.Username = dto.Username;
            if (!string.IsNullOrEmpty(dto.Role)) admin.Role = dto.Role;
            if (!string.IsNullOrEmpty(dto.Status)) admin.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.Password))
            {
                admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAdmin(AdminRegisterDto dto)
        {
            if (await _context.Admins.AnyAsync(a => a.Username == dto.Username))
            {
                return Conflict("Username already exists.");
            }

            var admin = new Admin
            {
                Username = dto.Username,
                Role = "Admin",
                Status = "Active"
            };

            admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.Password);
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();

            return Ok(new { admin.Id, admin.Username, admin.Role });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AdminAuthResponseDto>> Login(AdminLoginDto dto)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == dto.Username && a.Status == "Active");
            if (admin == null)
            {
                return Unauthorized("Invalid credentials.");
            }

            var result = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, admin.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new AdminAuthResponseDto
            {
                Token = tokenString,
                AdminId = admin.Id,
                Username = admin.Username,
                Role = admin.Role
            });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalRides = await _context.Rides.CountAsync();
            var totalDrivers = await _context.Driver.CountAsync();
            var totalVehicles = await _context.Vehicles.CountAsync();
            return Ok(new { totalUsers, totalRides, totalDrivers, totalVehicles });
        }

        [HttpGet("rides")]
        public async Task<IActionResult> GetRides()
        {
            var rides = await _context.Rides
                .Select(r => new RideDto
                {
                    Id = r.Id,
                    DriverId = r.DriverId,
                    VehicleId = r.VehicleId,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DepartureTime = r.DepartureTime,
                    TotalSeats = r.TotalSeats,
                    AvailableSeats = r.AvailableSeats,
                    DistanceKm = r.DistanceKm,
                    PricePerSeat = r.PricePerSeat,
                    Status = r.Status,
                    IsRecurring = r.IsRecurring
                })
                .ToListAsync();

            return Ok(rides);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.Id, u.Email, u.UserType, u.AccountStatus })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (dto.Email != null) user.Email = dto.Email;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.UserType != null) user.UserType = dto.UserType;
            if (dto.AccountStatus != null) user.AccountStatus = dto.AccountStatus;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
            }
            if (dto.FullName != null) user.UserProfile.FullName = dto.FullName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, UserStatusUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.AccountStatus = dto.Status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.UserReports
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportingUser)
                .ToListAsync();
            return Ok(reports);
        }

        [HttpPut("config")]
        public async Task<IActionResult> UpdateConfig(SystemConfigDto dto)
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == dto.Key);
            if (config == null)
            {
                config = new SystemConfiguration { Key = dto.Key, Value = dto.Value };
                _context.SystemConfigurations.Add(config);
            }
            else
            {
                config.Value = dto.Value;
            }
            await _context.SaveChangesAsync();
            return Ok(config);
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var metrics = await _context.Analytics.ToListAsync();
            return Ok(metrics);
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification(NotificationCreateDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
            {
                return NotFound("User not found");
            }

            var notification = new Notification
            {
                UserId = dto.UserId,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(CreateNotification), new { id = notification.Id }, notification);
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await _context.Driver
                .Join(_context.DriverProfiles,
                      d => d.DriverId,
                      dp => dp.DriverId,
                      (d, dp) => new DriverDto
                      {
                          Id = d.DriverId,
                          Email = d.Email,
                          PhoneNumber = d.PhoneNumber,
                          FullName = d.FullName,
                          LicenseNumber = dp.LicenseNumber,
                          BackgroundCheckStatus = dp.BackgroundCheckStatus,
                          DrivingExperienceYears = dp.DrivingExperienceYears,
                          DOB = DateOnly.FromDateTime(dp.DOB)
                      }).ToListAsync();

            return Ok(drivers);
        }

        [HttpPut("drivers/{id}")]
        public async Task<IActionResult> UpdateDriver(int id, DriverUpdateDto dto)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == id);
            if (driver == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.PhoneNumber != null) driver.PhoneNumber = dto.PhoneNumber;
            if (dto.FullName != null) driver.FullName = dto.FullName;

            if (dto.LicenseNumber != null) driverProfile.LicenseNumber = dto.LicenseNumber;
            if (dto.DrivingExperienceYears.HasValue) driverProfile.DrivingExperienceYears = dto.DrivingExperienceYears.Value;
            if (dto.DOB.HasValue) driverProfile.DOB = dto.DOB.Value;

            driverProfile.BackgroundCheckStatus = "Pending";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("drivers/{id}")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == id);
            if (driver == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile != null)
            {
                _context.DriverProfiles.Remove(driverProfile);
            }

            var vehicles = await _context.Vehicles.Where(v => v.DriverId == id).ToListAsync();
            if (vehicles.Any())
            {
                _context.Vehicles.RemoveRange(vehicles);
            }

            _context.Driver.Remove(driver);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("rides/{id}")]
        public async Task<IActionResult> GetRide(int id)
        {
            var ride = await _context.Rides
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new RideDto
                {
                    Id = r.Id,
                    DriverId = r.DriverId,
                    VehicleId = r.VehicleId,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DepartureTime = r.DepartureTime,
                    TotalSeats = r.TotalSeats,
                    AvailableSeats = r.AvailableSeats,
                    DistanceKm = r.DistanceKm,
                    PricePerSeat = r.PricePerSeat,
                    Status = r.Status,
                    IsRecurring = r.IsRecurring
                })
                .FirstOrDefaultAsync();

            if (ride == null)
            {
                return NotFound();
            }

            return Ok(ride);
        }

        [HttpGet("rides/{id}/driver")]
        public async Task<IActionResult> GetRideDriver(int id)
        {
            var ride = await _context.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var driver = await _context.Driver
                .Include(d => d.DriverProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DriverId == ride.DriverId);

            if (driver == null || driver.DriverProfile == null)
            {
                return NotFound("Driver not found");
            }

            var profile = driver.DriverProfile;

            var driverDto = new DriverDto
            {
                Id = driver.DriverId,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                FullName = driver.FullName,
                LicenseNumber = profile.LicenseNumber,
                BackgroundCheckStatus = profile.BackgroundCheckStatus,
                DrivingExperienceYears = profile.DrivingExperienceYears,
                DOB = DateOnly.FromDateTime(profile.DOB)
            };

            return Ok(driverDto);
        }

        [HttpGet("rides/{id}/vehicle")]
        public async Task<IActionResult> GetRideVehicle(int id)
        {
            var ride = await _context.Rides.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var vehicle = await _context.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == ride.VehicleId);

            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            var vehicleDto = new VehicleDto
            {
                Id = vehicle.Id,
                CarMaker = vehicle.CarMaker,
                Model = vehicle.Model,
                VehicleType = vehicle.VehicleType,
                LicensePlate = vehicle.LicensePlate,
                Year = vehicle.Year,
                VerificationStatus = vehicle.VerificationStatus
            };

            return Ok(vehicleDto);
        }

        [HttpPut("rides/{id}")]
        public async Task<IActionResult> UpdateRide(int id, [FromBody] RideUpdateDto dto)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound();
            }

            ride.Origin = dto.Origin;
            ride.Destination = dto.Destination;
            ride.DepartureTime = dto.DepartureTime;
            ride.DistanceKm = dto.DistanceKm;
            ride.AvailableSeats += dto.TotalSeats - ride.TotalSeats;
            ride.TotalSeats = dto.TotalSeats;

            var existingRoutePoints = _context.RoutePoints.Where(rp => rp.RideId == id);
            _context.RoutePoints.RemoveRange(existingRoutePoints);

            if (dto.RoutePoints != null && dto.RoutePoints.Count > 0)
            {
                foreach (var rp in dto.RoutePoints)
                {
                    _context.RoutePoints.Add(new RoutePoint
                    {
                        RideId = id,
                        Location = rp.Location,
                        SequenceNumber = rp.SequenceNumber
                    });
                }
            }

            await _context.SaveChangesAsync();

            var result = new RideDto
            {
                Id = ride.Id,
                DriverId = ride.DriverId,
                VehicleId = ride.VehicleId,
                Origin = ride.Origin,
                Destination = ride.Destination,
                DepartureTime = ride.DepartureTime,
                TotalSeats = ride.TotalSeats,
                AvailableSeats = ride.AvailableSeats,
                DistanceKm = ride.DistanceKm,
                PricePerSeat = ride.PricePerSeat,
                Status = ride.Status,
                IsRecurring = ride.IsRecurring
            };

            return Ok(result);
        }

        [HttpPut("rides/{id}/driver")]
        public async Task<IActionResult> UpdateRideDriver(int id, [FromBody] RideDriverUpdateDto dto)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == dto.DriverId);
            if (driver == null)
            {
                return NotFound("Driver not found");
            }

            ride.DriverId = dto.DriverId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("rides/{id}/vehicle")]
        public async Task<IActionResult> UpdateRideVehicle(int id, [FromBody] RideVehicleUpdateDto dto)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.Id == id);
            if (ride == null)
            {
                return NotFound("Ride not found");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId);
            if (vehicle == null)
            {
                return NotFound("Vehicle not found");
            }

            if (vehicle.DriverId != ride.DriverId)
            {
                return BadRequest("Vehicle does not belong to the ride's driver");
            }

            ride.VehicleId = dto.VehicleId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("rides/{id}")]
        public async Task<IActionResult> DeleteRide(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            if (ride == null)
            {
                return NotFound();
            }

            var routePoints = _context.RoutePoints.Where(rp => rp.RideId == id);
            _context.RoutePoints.RemoveRange(routePoints);

            var recurrences = _context.RideRecurrences.Where(rr => rr.RideId == id);
            _context.RideRecurrences.RemoveRange(recurrences);

            _context.Rides.Remove(ride);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var vehicles = await _context.Vehicles
                .Select(v => new
                {
                    v.Id,
                    v.DriverId,
                    v.CarMaker,
                    v.Model,
                    v.VehicleType,
                    v.LicensePlate,
                    v.Year,
                    v.VerificationStatus
                }).ToListAsync();

            return Ok(vehicles);
        }

        [HttpPut("vehicles/{id}")]
        public async Task<IActionResult> UpdateVehicle(int id, VehicleUpdateDto dto)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            if (dto.CarMaker != null) vehicle.CarMaker = dto.CarMaker;
            if (dto.Model != null) vehicle.Model = dto.Model;
            if (dto.VehicleType != null) vehicle.VehicleType = dto.VehicleType;
            if (dto.LicensePlate != null) vehicle.LicensePlate = dto.LicensePlate;
            if (dto.Year.HasValue) vehicle.Year = dto.Year.Value;
            if (dto.VerificationStatus != null) vehicle.VerificationStatus = dto.VerificationStatus;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("vehicles/{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

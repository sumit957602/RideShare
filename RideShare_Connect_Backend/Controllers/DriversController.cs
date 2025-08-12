using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using RideShare_Connect_Backend.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Driver> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;

        public DriversController(ApplicationDbContext context, IPasswordHasher<Driver> passwordHasher, IConfiguration configuration, IEmailService emailService, IDistributedCache cache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<DriverDto>> RegisterDriver([FromForm] DriverRegisterDto dto)
        {
            if (await _context.Driver.AnyAsync(d => d.Email == dto.Email))
            {
                return Conflict("Driver with this email already exists.");
            }

            var driver = new Driver
            {
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FullName = dto.FullName
            };
            driver.PasswordHash = _passwordHasher.HashPassword(driver, dto.Password);

            _context.Driver.Add(driver);
            await _context.SaveChangesAsync();

            var driverProfile = new DriverProfile
            {
                DriverId = driver.DriverId,
                LicenseNumber = dto.LicenseNumber,
                BackgroundCheckStatus = "Pending",
                DrivingExperienceYears = dto.DrivingExperienceYears,
                DOB = dto.DOB
            };
            _context.DriverProfiles.Add(driverProfile);

            await _context.SaveChangesAsync();

            var driverDto = new DriverDto
            {
                Id = driver.DriverId,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                FullName = driver.FullName,
                LicenseNumber = driverProfile.LicenseNumber,
                BackgroundCheckStatus = driverProfile.BackgroundCheckStatus,
                DrivingExperienceYears = driverProfile.DrivingExperienceYears,
                DOB = DateOnly.FromDateTime(driverProfile.DOB)
            };
            return CreatedAtAction(nameof(GetDriver), new { id = driverDto.Id }, driverDto);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginDriver(LoginDto dto)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.Email == dto.Email);
            if (driver == null)
            {
                return Unauthorized("Invalid credentials.");
            }
            var result = _passwordHasher.VerifyHashedPassword(driver, driver.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, driver.DriverId.ToString()),
                new Claim(ClaimTypes.Email, driver.Email),
                new Claim(ClaimTypes.Role, "Driver")
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
            var response = new AuthResponseDto
            {
                Token = tokenString,
                RefreshToken = string.Empty,
                UserId = driver.DriverId,
                Email = driver.Email,
                FullName = driver.FullName,
                UserType = "Driver"
            };
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (driver == null)
            {
                // For security, don't reveal if a user exists or not.
                return Ok("If your account exists, a password reset OTP has been sent to your email.");
            }

            // Generate a 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"PasswordReset-Otp-{driver.Email}";
            var expiration = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // OTP valid for 5 minutes
            };

            // Store the OTP in the cache
            await _cache.SetStringAsync(cacheKey, otp, expiration);

            // Send the OTP via email
            await _emailService.SendOtpEmailAsync(driver.Email, driver?.FullName ?? "User", otp, "PasswordReset");

            return Ok("Password reset OTP sent to your email.");
        }


        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            // Validate OTP from cache
            var cacheKey = $"PasswordReset-Otp-{dto.Email}";
            var storedOtp = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Otp)
            {
                return BadRequest("Invalid or expired OTP.");
            }

            var driver = await _context.Driver.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (driver == null)
            {
                return NotFound("User not found.");
            }

            driver.PasswordHash = _passwordHasher.HashPassword(driver, dto.NewPassword);

            // Remove the OTP from the cache after successful use
            await _cache.RemoveAsync(cacheKey);

            await _context.SaveChangesAsync();

            return Ok("Password reset successful.");
        }


        [HttpPost("register-vehicle")]
        public async Task<IActionResult> RegisterVehicle([FromQuery] int driverId, [FromBody] VehicleRegisterDto dto)
        {
            var driver = await _context.Driver.FindAsync(driverId);
            if (driver == null)
            {
                return NotFound("Driver not found");
            }

            var vehicle = new Vehicle
            {
                DriverId = driverId,
                CarMaker = dto.CarMaker,
                Model = dto.Model,
                VehicleType = dto.VehicleType,
                LicensePlate = dto.LicensePlate,
                VerificationStatus = "Pending",
                Year = dto.Year
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            var result = new VehicleDto
            {
                Id = vehicle.Id,
                CarMaker = vehicle.CarMaker,
                Model = vehicle.Model,
                VehicleType = vehicle.VehicleType,
                LicensePlate = vehicle.LicensePlate,
                Year = vehicle.Year,
                VerificationStatus = vehicle.VerificationStatus
            };

            return CreatedAtAction(nameof(RegisterVehicle), new { id = result.Id }, result);
        }


        [HttpGet("profile")]
        public async Task<ActionResult<DriverDto>> GetProfile([FromQuery] int driverId)
        {
            var driverProfile = await _context.DriverProfiles
                .Include(dp => dp.Driver)
                .AsNoTracking()
                .FirstOrDefaultAsync(dp => dp.DriverId == driverId);

            if (driverProfile?.Driver == null)
            {
                return NotFound();
            }

            var driverDto = new DriverDto
            {
                Id = driverProfile.DriverId,
                Email = driverProfile.Driver.Email,
                PhoneNumber = driverProfile.Driver.PhoneNumber,
                FullName = driverProfile.Driver.FullName,
                LicenseNumber = driverProfile.LicenseNumber,
                BackgroundCheckStatus = driverProfile.BackgroundCheckStatus,
                DrivingExperienceYears = driverProfile.DrivingExperienceYears,
                DOB = DateOnly.FromDateTime(driverProfile.DOB)
            };

            return Ok(driverDto);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromQuery] int driverId, DriverProfileUpdateDto dto)
        {
            var driverProfile = await _context.DriverProfiles
                .Include(dp => dp.Driver)
                .FirstOrDefaultAsync(dp => dp.DriverId == driverId);

            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.PhoneNumber != null) driverProfile.Driver.PhoneNumber = dto.PhoneNumber;
            if (dto.FullName != null) driverProfile.Driver.FullName = dto.FullName;

            if (dto.LicenseNumber != null) driverProfile.LicenseNumber = dto.LicenseNumber;
            if (dto.DrivingExperienceYears.HasValue) driverProfile.DrivingExperienceYears = dto.DrivingExperienceYears.Value;
            if (dto.DOB.HasValue) driverProfile.DOB = dto.DOB.Value;

            driverProfile.BackgroundCheckStatus = "Pending";

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("ratings")]
        public async Task<IActionResult> GetRatings([FromQuery] int driverId)
        {
            var ratings = await _context.DriverRatings
                .Where(r => r.DriverId == driverId)
                .Select(r => new DriverRatingDto
                {
                    Id = r.Id,
                    PassengerId = r.PassengerId,
                    RideId = r.RideId,
                    Rating = r.Rating,
                    Review = r.Review,
                    Timestamp = r.Timestamp
                }).ToListAsync();

            var average = ratings.Any() ? ratings.Average(r => r.Rating) : 0;

            return Ok(new { averageRating = average, ratings });
        }

        // GET: api/Drivers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverSummaryDto>>> GetDrivers()
        {
            var drivers = await _context.Driver
                .Select(d => new DriverSummaryDto
                {
                    Id = d.DriverId,
                    Email = d.Email
                })
                .ToListAsync();

            return Ok(drivers);
        }

        // GET: api/Drivers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DriverDto>> GetDriver(int id)
        {
            var driver = await _context.Driver
                .Where(d => d.DriverId == id)
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
                      })
                .FirstOrDefaultAsync();

            if (driver == null)
            {
                return NotFound();
            }

            return Ok(driver);
        }

        // PUT: api/Drivers/{id}
        [HttpPut("{id}")]
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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DriverExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Drivers/{id}/account
        [HttpDelete("{id}/account")]
        public async Task<IActionResult> DeleteDriverAccount(int id, [FromBody] DeleteAccountDto dto)
        {
            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            var verify = _passwordHasher.VerifyHashedPassword(driver, driver.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile != null)
            {
                _context.DriverProfiles.Remove(driverProfile);
            }

            _context.Driver.Remove(driver);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DriverExists(int id)
        {
            return _context.Driver.Any(e => e.DriverId == id);
        }
    }
}

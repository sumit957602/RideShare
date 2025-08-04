using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Linq;
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

        public DriversController(ApplicationDbContext context, IPasswordHasher<Driver> passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
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
                ReferenceContacts = dto.ReferenceContacts
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
                ReferenceContacts = driverProfile.ReferenceContacts
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

        [Authorize(Roles = "Driver")]
        [HttpGet("profile")]
        public async Task<ActionResult<DriverDto>> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

            var driver = await _context.Driver
                .Include(d => d.DriverProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver?.DriverProfile == null)
            {
                return NotFound();
            }

            var driverDto = new DriverDto
            {
                Id = driver.DriverId,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                FullName = driver.FullName,
                LicenseNumber = driver.DriverProfile.LicenseNumber,
                BackgroundCheckStatus = driver.DriverProfile.BackgroundCheckStatus,
                DrivingExperienceYears = driver.DriverProfile.DrivingExperienceYears,
                ReferenceContacts = driver.DriverProfile.ReferenceContacts
            };

            return Ok(driverDto);
        }

        [Authorize(Roles = "Driver")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(DriverUpdateDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

            var driver = await _context.Driver.FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (driver == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == driverId);
            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.Email != null) driver.Email = dto.Email;
            if (dto.PhoneNumber != null) driver.PhoneNumber = dto.PhoneNumber;
            if (dto.FullName != null) driver.FullName = dto.FullName;

            if (dto.LicenseNumber != null) driverProfile.LicenseNumber = dto.LicenseNumber;
            if (dto.BackgroundCheckStatus != null) driverProfile.BackgroundCheckStatus = dto.BackgroundCheckStatus;
            if (dto.DrivingExperienceYears.HasValue) driverProfile.DrivingExperienceYears = dto.DrivingExperienceYears.Value;
            if (dto.ReferenceContacts != null) driverProfile.ReferenceContacts = dto.ReferenceContacts;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize(Roles = "Driver")]
        [HttpGet("ratings")]
        public async Task<IActionResult> GetRatings()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var driverId = int.Parse(userIdClaim);

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
        public async Task<ActionResult<IEnumerable<DriverDto>>> GetDrivers()
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
                          ReferenceContacts = dp.ReferenceContacts
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
                          ReferenceContacts = dp.ReferenceContacts
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

            if (dto.Email != null) driver.Email = dto.Email;
            if (dto.PhoneNumber != null) driver.PhoneNumber = dto.PhoneNumber;
            if (dto.FullName != null) driver.FullName = dto.FullName;

            if (dto.LicenseNumber != null) driverProfile.LicenseNumber = dto.LicenseNumber;
            if (dto.BackgroundCheckStatus != null) driverProfile.BackgroundCheckStatus = dto.BackgroundCheckStatus;
            if (dto.DrivingExperienceYears.HasValue) driverProfile.DrivingExperienceYears = dto.DrivingExperienceYears.Value;
            if (dto.ReferenceContacts != null) driverProfile.ReferenceContacts = dto.ReferenceContacts;

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

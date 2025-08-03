using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.Models.VehicleManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.IO;
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
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public DriversController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> RegisterDriver([FromForm] DriverRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return Conflict("User with this email already exists.");
            }

            var user = new User
            {
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                UserType = "Driver",
                AccountStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userProfile = new UserProfile
            {
                UserId = user.Id,
                FullName = dto.FullName,
                Bio = "Default bio message for new drivers.",
                ProfilePicture = "https://avatar.iran.liara.run/public/boy?username=Driver",
                EmergencyContact = "Nan",
            };
            _context.UserProfiles.Add(userProfile);

            var driverProfile = new DriverProfile
            {
                DriverId = user.Id,
                LicenseNumber = dto.LicenseNumber,
                BackgroundCheckStatus = "Pending",
                DrivingExperienceYears = dto.DrivingExperienceYears,
                ReferenceContacts = dto.ReferenceContacts
            };
            _context.DriverProfiles.Add(driverProfile);

            await _context.SaveChangesAsync();

            if (dto.LicenseDocument != null && dto.LicenseDocument.Length > 0)
            {
                var uploadsRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var folder = Path.Combine(uploadsRoot, "documents");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid()}_{dto.LicenseDocument.FileName}";
                var filePath = Path.Combine(folder, fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    await dto.LicenseDocument.CopyToAsync(stream);
                }

                var verification = new UserVerification
                {
                    UserId = user.Id,
                    DocUrl = $"/documents/{fileName}",
                    VerificationType = "driving-license",
                    Status = "Pending",
                    IssuedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow.AddYears(1)
                };
                _context.UserVerifications.Add(verification);
                await _context.SaveChangesAsync();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = dto.FullName,
                UserType = user.UserType,
                AccountStatus = user.AccountStatus,
                CreatedAt = user.CreatedAt,
                IsVerified = user.IsVerified,
                VerificationBadge = user.VerificationBadge
            };
            return CreatedAtAction(nameof(RegisterDriver), new { id = userDto.Id }, userDto);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginDriver(LoginDto dto)
        {
            var user = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Email == dto.Email && u.UserType == "Driver");
            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType)
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
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                UserType = user.UserType,
                IsVerified = user.IsVerified,
                VerificationBadge = user.VerificationBadge
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
            var userId = int.Parse(userIdClaim);

            var driver = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.UserType == "Driver" && u.Id == userId)
                .Join(_context.DriverProfiles,
                      u => u.Id,
                      dp => dp.DriverId,
                      (u, dp) => new DriverDto
                      {
                          Id = u.Id,
                          Email = u.Email,
                          PhoneNumber = u.PhoneNumber,
                          FullName = u.UserProfile != null ? u.UserProfile.FullName : null,
                          UserType = u.UserType,
                          AccountStatus = u.AccountStatus,
                          CreatedAt = u.CreatedAt,
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

        [Authorize(Roles = "Driver")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(DriverUpdateDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var id = int.Parse(userIdClaim);

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Driver");

            if (user == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.Email != null) user.Email = dto.Email;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.AccountStatus != null) user.AccountStatus = dto.AccountStatus;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
            }
            if (dto.FullName != null) user.UserProfile.FullName = dto.FullName;

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
            var id = int.Parse(userIdClaim);

            var ratings = await _context.DriverRatings
                .Where(r => r.DriverId == id)
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
            var drivers = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.UserType == "Driver")
                .Join(_context.DriverProfiles,
                      u => u.Id,
                      dp => dp.DriverId,
                      (u, dp) => new DriverDto
                      {
                          Id = u.Id,
                          Email = u.Email,
                          PhoneNumber = u.PhoneNumber,
                          FullName = u.UserProfile != null ? u.UserProfile.FullName : null,
                          UserType = u.UserType,
                          AccountStatus = u.AccountStatus,
                          CreatedAt = u.CreatedAt,
                          IsVerified = u.IsVerified,
                          VerificationBadge = u.VerificationBadge,
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
            var driver = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.UserType == "Driver" && u.Id == id)
                .Join(_context.DriverProfiles,
                      u => u.Id,
                      dp => dp.DriverId,
                      (u, dp) => new DriverDto
                      {
                          Id = u.Id,
                          Email = u.Email,
                          PhoneNumber = u.PhoneNumber,
                          FullName = u.UserProfile != null ? u.UserProfile.FullName : null,
                          UserType = u.UserType,
                          AccountStatus = u.AccountStatus,
                          CreatedAt = u.CreatedAt,
                          IsVerified = u.IsVerified,
                          VerificationBadge = u.VerificationBadge,
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
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Driver");

            if (user == null)
            {
                return NotFound();
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile == null)
            {
                return NotFound();
            }

            if (dto.Email != null) user.Email = dto.Email;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.AccountStatus != null) user.AccountStatus = dto.AccountStatus;

            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile { UserId = user.Id };
            }
            if (dto.FullName != null) user.UserProfile.FullName = dto.FullName;

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
                if (!UserExists(id))
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == "Driver");

            if (user == null)
            {
                return NotFound();
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(dp => dp.DriverId == id);
            if (driverProfile != null)
            {
                _context.DriverProfiles.Remove(driverProfile);
            }

            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == id);
            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}

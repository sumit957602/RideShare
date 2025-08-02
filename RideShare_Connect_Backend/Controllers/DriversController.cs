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
            user.UserProfile = new UserProfile
            {
                FullName = dto.FullName,
                Bio = "Default bio message for new drivers.",
                ProfilePicture = "https://avatar.iran.liara.run/public/boy?username=Driver",
                EmergencyContact = "Nan",
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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
                    Status = "Uploaded",
                    IssuedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow
                };
                _context.UserVerifications.Add(verification);
                await _context.SaveChangesAsync();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.UserProfile.FullName,
                UserType = user.UserType,
                AccountStatus = user.AccountStatus,
                CreatedAt = user.CreatedAt
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
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                UserType = user.UserType
            };
            return Ok(response);
        }
    }
}

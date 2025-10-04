using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.VehicleManagement;
using RideShareConnect.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RideShare_Connect.Services
{
    public class DriverAccountService : IDriverAccountService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Driver> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;

        public DriverAccountService(
            AppDbContext context,
            IPasswordHasher<Driver> passwordHasher,
            IConfiguration configuration,
            IEmailService emailService,
            IDistributedCache cache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task RegisterAsync(DriverRegisterDto dto)
        {
            if (await _context.Set<Driver>().AnyAsync(d => d.Email == dto.Email))
            {
                throw new ServiceException("Driver with this email already exists.");
            }

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                throw new ServiceException("Phone number is required.");
            }

            var driver = new Driver
            {
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FullName = dto.FullName
            };

            driver.PasswordHash = _passwordHasher.HashPassword(driver, dto.Password ?? string.Empty);

            _context.Set<Driver>().Add(driver);
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
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var driver = await _context.Set<Driver>().FirstOrDefaultAsync(d => d.Email == dto.Email);
            if (driver == null)
            {
                throw new ServiceException("Invalid credentials.");
            }

            var result = _passwordHasher.VerifyHashedPassword(driver, driver.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new ServiceException("Invalid credentials.");
            }

            var token = GenerateJwtToken(driver.DriverId, driver.Email, "Driver");

            return new AuthResponseDto
            {
                Token = token,
                UserId = driver.DriverId,
                Email = driver.Email,
                FullName = driver.FullName,
                UserType = "Driver"
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var driver = await _context.Set<Driver>().FirstOrDefaultAsync(d => d.Email == dto.Email);
            if (driver == null)
            {
                return;
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"PasswordReset-Otp-{driver.Email}";

            await _cache.SetStringAsync(
                cacheKey,
                otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await _emailService.SendOtpEmailAsync(driver.Email, driver?.FullName ?? "Driver", otp, "PasswordReset");
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var cacheKey = $"PasswordReset-Otp-{dto.Email}";
            var storedOtp = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Otp)
            {
                throw new ServiceException("Invalid or expired OTP.");
            }

            var driver = await _context.Set<Driver>().FirstOrDefaultAsync(d => d.Email == dto.Email);
            if (driver == null)
            {
                throw new ServiceException("User not found.");
            }

            driver.PasswordHash = _passwordHasher.HashPassword(driver, dto.NewPassword ?? string.Empty);
            await _cache.RemoveAsync(cacheKey);
            await _context.SaveChangesAsync();
        }

        public async Task<Vehicle> RegisterVehicleAsync(int driverId, VehicleRegisterDto dto)
        {
            var driver = await _context.Set<Driver>().FirstOrDefaultAsync(d => d.DriverId == driverId);
            if (driver == null)
            {
                throw new ServiceException("Driver not found.");
            }

            var vehicle = new Vehicle
            {
                DriverId = driverId,
                CarMaker = dto.CarMaker,
                Model = dto.CarModel,
                VehicleType = dto.VehicleType,
                LicensePlate = dto.LicensePlate,
                Year = dto.Year,
                VerificationStatus = "Pending"
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return vehicle;
        }

        private string GenerateJwtToken(int id, string email, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrEmpty(secret))
            {
                throw new ServiceException("JWT configuration is missing.");
            }

            var key = Encoding.ASCII.GetBytes(secret);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationMinutes"] ?? "60")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}

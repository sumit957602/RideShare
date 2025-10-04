using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.AdminManagement;
using RideShareConnect.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RideShare_Connect.Services
{
    public class AdminAccountService : IAdminAccountService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Admin> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AdminAccountService(AppDbContext context, IPasswordHasher<Admin> passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == dto.Username && a.Status == "Active");
            if (admin == null)
            {
                throw new ServiceException("Invalid credentials.");
            }

            var result = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new ServiceException("Invalid credentials.");
            }

            var token = GenerateJwtToken(admin.Id, admin.Username, admin.Role);

            return new AdminAuthResponseDto
            {
                Token = token,
                AdminId = admin.Id,
                Username = admin.Username,
                Role = admin.Role
            };
        }

        private string GenerateJwtToken(int id, string username, string role)
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
                new Claim(ClaimTypes.Name, username),
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.UserManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RideShare_Connect.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == int.Parse(userIdClaim));
            if (user == null)
            {
                return Unauthorized();
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.UserType ?? "Rider")
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

            var responseDto = new AuthResponseDto
            {
                Token = tokenString,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture = user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };

            return Ok(responseDto);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var record = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            _context.PasswordResetTokens.Add(record);
            await _context.SaveChangesAsync();

            // In production, send the token via email. Here we return it for testing.
            return Ok(new { Token = token });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var record = await _context.PasswordResetTokens.Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == dto.Token && r.ExpiresAt > DateTime.UtcNow);
            if (record == null)
            {
                return BadRequest("Invalid or expired token.");
            }

            var user = record.User;
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            _context.PasswordResetTokens.Remove(record);
            await _context.SaveChangesAsync();

            return Ok("Password reset successful.");
        }
    }
}

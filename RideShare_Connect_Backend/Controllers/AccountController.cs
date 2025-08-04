// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.IdentityModel.Tokens; // Required for security tokens
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.UserManagement;
using System.IdentityModel.Tokens.Jwt; // Required for JwtSecurityTokenHandler
using System.Security.Claims; // Required for Claims
using System.Text; // Required for Encoding
using System.IO;

namespace RideShare_Connect.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // If you uncomment this, you'll need [AllowAnonymous] on Register and Login
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration; // Inject IConfiguration
        private readonly IWebHostEnvironment _environment;

        public UsersController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration; // Initialize IConfiguration
            _environment = environment;
        }

        // GET: api/Users
        // This endpoint would typically require authorization to list all users
        // [Authorize] // Uncomment this to protect this endpoint
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users
                                    .Include(u => u.UserProfile)
                                    .Select(u => new UserDto
                                    {
                                        Id = u.Id,
                                        Email = u.Email,
                                        PhoneNumber = u.PhoneNumber,
                                        FullName = u.UserProfile != null ? u.UserProfile.FullName : null,
                                        UserType = u.UserType,
                                        AccountStatus = u.AccountStatus,
                                        CreatedAt = u.CreatedAt
                                    })
                                    .ToListAsync();
            return Ok(users);
        }

        // GET: api/Users/5
        // This endpoint would typically require authorization for specific user data
        // [Authorize] // Uncomment this to protect this endpoint
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _context.Users
                                    .Include(u => u.UserProfile)
                                    .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.UserProfile != null ? user.UserProfile.FullName : null,
                UserType = user.UserType,
                AccountStatus = user.AccountStatus,
                CreatedAt = user.CreatedAt
            };

            return Ok(userDto);
        }

        // POST: api/Users/register
        // This endpoint should *not* require authorization as new users won't have a token.
        [AllowAnonymous] // <--- IMPORTANT: Allow unauthenticated access
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> RegisterUser(UserRegisterDto userRegisterDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userRegisterDto.Email))
            {
                return Conflict("User with this email already exists.");
            }

            var newUser = new User
            {
                Email = userRegisterDto.Email,
                PhoneNumber = userRegisterDto.PhoneNumber,
                UserType = userRegisterDto.UserType ?? "Rider",
                AccountStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, userRegisterDto.Password);

            newUser.UserProfile = new UserProfile
            {
                FullName = userRegisterDto.FullName,
                Bio = "Default bio message for new users.",
                ProfilePicture = "https://avatar.iran.liara.run/public/boy?username=Ash",
                EmergencyContact = "Nan",
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                FullName = newUser.UserProfile.FullName,
                UserType = newUser.UserType,
                AccountStatus = newUser.AccountStatus,
                CreatedAt = newUser.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
        }

        // POST: api/Users/login
        // This endpoint should also *not* require authorization.
        [AllowAnonymous] // <--- IMPORTANT: Allow unauthenticated access
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var normalizedEmail = loginDto.Email?.Trim().ToLower();

            var user = await _context.Users
                                     .Include(u => u.UserProfile) // Include UserProfile for FullName
                                     .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid credentials.");
            }

            // --- Generate JWT Token ---
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
            // --- End JWT Token Generation ---

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenDays"] ?? "7")),
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var responseDto = new AuthResponseDto
            {
                Token = tokenString,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture = user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };

            return Ok(responseDto);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var existing = await _context.RefreshTokens
                .Include(r => r.User)
                .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.ExpiresAt > DateTime.UtcNow);

            if (existing == null)
            {
                return Unauthorized();
            }

            var user = existing.User;
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

            var newRefresh = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                ExpiresAt = DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenDays"] ?? "7")),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Remove(existing);
            _context.RefreshTokens.Add(newRefresh);
            await _context.SaveChangesAsync();

            var responseDto = new AuthResponseDto
            {
                Token = tokenString,
                RefreshToken = newRefresh.Token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture = user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };

            return Ok(responseDto);
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile([FromQuery] int userId)
        {
            var user = await _context.Users
                                     .Include(u => u.UserProfile)
                                     .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = user.UserProfile?.FullName,
                UserType = user.UserType,
                AccountStatus = user.AccountStatus,
                CreatedAt = user.CreatedAt
            };

            return Ok(userDto);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromQuery] int userId, UserUpdateDto dto)
        {
            var user = await _context.Users
                                     .Include(u => u.UserProfile)
                                     .FirstOrDefaultAsync(u => u.Id == userId);
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

        [Authorize]
        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            var type = dto.DocumentType?.ToLower();
            if (type != "id-proof" && type != "driving-license")
            {
                return BadRequest("Unsupported document type.");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized();
            }
            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var uploadsRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(uploadsRoot, "documents");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}_{dto.File.FileName}";
            var filePath = Path.Combine(folder, fileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                await dto.File.CopyToAsync(stream);
            }

            var record = new UserVerification
            {
                UserId = userId,
                DocUrl = $"/documents/{fileName}",
                VerificationType = type,
                Status = "Uploaded",
                IssuedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow
            };
            _context.UserVerifications.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { record.Id, record.DocUrl, record.VerificationType });
        }

        // PUT: api/Users/5
        // This endpoint should typically require authorization for a user to update their own profile
        // [Authorize] // Uncomment this to protect this endpoint
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {
            // Optional: Add logic to ensure the logged-in user can only update their own profile
            // if (User.FindFirst(ClaimTypes.NameIdentifier)?.Value != id.ToString())
            // {
            //     return Forbid(); // Or Unauthorized if no token, Forbidden if token exists but user isn't allowed
            // }

            var userToUpdate = await _context.Users
                                            .Include(u => u.UserProfile)
                                            .FirstOrDefaultAsync(u => u.Id == id);

            if (userToUpdate == null)
            {
                return NotFound();
            }

            if (userUpdateDto.Email != null) userToUpdate.Email = userUpdateDto.Email;
            if (userUpdateDto.PhoneNumber != null) userToUpdate.PhoneNumber = userUpdateDto.PhoneNumber;
            if (userUpdateDto.UserType != null) userToUpdate.UserType = userUpdateDto.UserType;
            if (userUpdateDto.AccountStatus != null) userToUpdate.AccountStatus = userUpdateDto.AccountStatus;

            if (userToUpdate.UserProfile == null)
            {
                userToUpdate.UserProfile = new UserProfile { UserId = userToUpdate.Id };
            }
            if (userUpdateDto.FullName != null) userToUpdate.UserProfile.FullName = userUpdateDto.FullName;


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

        // DELETE: api/Users/5
        // This endpoint should typically require authorization, often with specific roles (e.g., Admin)
        // [Authorize(Roles = "Admin")] // Example: Only Admin can delete users
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Users/{id}/account
        // Allows a user to permanently remove their account by confirming their password
        [HttpDelete("{id}/account")]
        public async Task<IActionResult> DeleteAccount(int id, [FromBody] DeleteAccountDto deleteDto)
        {
            var user = await _context.Users
                                     .Include(u => u.UserProfile)
                                     .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, deleteDto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid password.");
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
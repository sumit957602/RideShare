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
using RideShare_Connect_Backend.Services;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;
        public UsersController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration, IWebHostEnvironment environment, IEmailService emailService, IDistributedCache cache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration; // Initialize IConfiguration
            _environment = environment;
            _emailService = emailService;
            _cache = cache;
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

        [AllowAnonymous]
        [HttpPost("send-verification-otp")]
        public async Task<IActionResult> SendVerificationOtp([FromBody] UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return Conflict("User with this email already exists.");
            }

            // Generate a 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"Registration-Otp-{dto.Email}";
            var expiration = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // OTP valid for 5 minutes
            };

            // Store the OTP in the cache (key: email, value: otp)
            await _cache.SetStringAsync(cacheKey, otp, expiration);

            // Send the OTP via email
            await _emailService.SendOtpEmailAsync(dto.Email, dto.FullName ?? "New User", otp, "Registration");

            return Ok("An OTP has been sent to your email for verification.");
        }

        // POST: api/Users/register
        // This endpoint should *not* require authorization as new users won't have a token.
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> RegisterUser(UserRegisterDto userRegisterDto)
        {
            // Validate OTP from cache
            var cacheKey = $"Registration-Otp-{userRegisterDto.Email}";
            var storedOtp = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != userRegisterDto.Otp)
            {
                return BadRequest("Invalid or expired OTP.");
            }

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
            await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.UserProfile.FullName);

            // Remove OTP from cache after successful registration
            await _cache.RemoveAsync(cacheKey);

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
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                // For security, don't reveal if a user exists or not.
                return Ok("If your account exists, a password reset OTP has been sent to your email.");
            }

            // Generate a 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"PasswordReset-Otp-{user.Email}";
            var expiration = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // OTP valid for 5 minutes
            };

            // Store the OTP in the cache
            await _cache.SetStringAsync(cacheKey, otp, expiration);

            // Send the OTP via email
            await _emailService.SendOtpEmailAsync(user.Email, user.UserProfile?.FullName ?? "User", otp, "PasswordReset");

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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);

            // Remove the OTP from the cache after successful use
            await _cache.RemoveAsync(cacheKey);

            await _context.SaveChangesAsync();

            return Ok("Password reset successful.");
        }

        [AllowAnonymous]
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile([FromQuery] int userId)
        {
            var profile = await _context.UserProfiles
                                         .FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return NotFound();
            }

            var profileDto = new UserProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FullName = profile.FullName,
                ProfilePicture = profile.ProfilePicture,
                EmergencyContact = profile.EmergencyContact,
                Bio = profile.Bio
            };

            return Ok(profileDto);
        }

        [AllowAnonymous]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromQuery] int userId, UserProfileUpdateDto dto)
        {
            var profile = await _context.UserProfiles
                                         .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                _context.UserProfiles.Add(profile);
            }

            if (dto.FullName != null) profile.FullName = dto.FullName;
            if (dto.ProfilePicture != null) profile.ProfilePicture = dto.ProfilePicture;
            if (dto.EmergencyContact != null) profile.EmergencyContact = dto.EmergencyContact;
            if (dto.Bio != null) profile.Bio = dto.Bio;

            await _context.SaveChangesAsync();
            return NoContent();
        }


        // PUT: api/Users/5
        // This endpoint should typically require authorization for a user to update their own profile
        // [Authorize] // Uncomment this to protect this endpoint
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto userUpdateDto)
        {

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
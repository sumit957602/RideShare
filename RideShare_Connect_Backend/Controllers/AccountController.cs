// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.IdentityModel.Tokens; // Required for security tokens
using Google.Apis.Auth;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using RideShare_Connect.Models.UserManagement;
using System.IdentityModel.Tokens.Jwt; // Required for JwtSecurityTokenHandler
using System.Security.Claims; // Required for Claims
using System.Text; // Required for Encoding
using System.IO;
using RideShare_Connect_Backend.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Net.Http;
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

        private class PendingRegistration
        {
            public UserRegisterDto User { get; set; }
            public string Otp { get; set; }
        }

        // GET: api/Users
        // This endpoint would typically require authorization to list all users
        // [Authorize] // Uncomment this to protect this endpoint
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers()
        {
            var users = await _context.Users
                                    .Select(u => new UserSummaryDto
                                    {
                                        Id = u.Id,
                                        Email = u.Email
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

            // Ensure user type defaults to Rider if not provided
            dto.UserType = string.IsNullOrWhiteSpace(dto.UserType) ? "Rider" : dto.UserType;

            // Generate a 6-digit OTP and a registration id
            var otp = new Random().Next(100000, 999999).ToString();
            var registrationId = Guid.NewGuid().ToString();

            var pending = new PendingRegistration
            {
                User = dto,
                Otp = otp
            };

            var cacheKey = $"Registration-{registrationId}";
            var expiration = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // OTP valid for 5 minutes
            };

            var json = JsonSerializer.Serialize(pending);
            await _cache.SetStringAsync(cacheKey, json, expiration);

            // Send the OTP via email
            await _emailService.SendOtpEmailAsync(dto.Email, dto.FullName ?? "New User", otp, "Registration");

            return Ok(new { Id = registrationId, Message = "An OTP has been sent to your email for verification." });
        }

        // POST: api/Users/register
        // This endpoint should *not* require authorization as new users won't have a token.
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> RegisterUser(UserRegisterVerifyDto verificationDto)
        {
            var cacheKey = $"Registration-{verificationDto.RegistrationId}";
            var registrationJson = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(registrationJson))
            {
                return BadRequest("Invalid or expired registration.");
            }

            var pending = JsonSerializer.Deserialize<PendingRegistration>(registrationJson);
            if (pending == null || pending.Otp != verificationDto.Otp)
            {
                return BadRequest("Invalid or expired OTP.");
            }

            var userRegisterDto = pending.User;

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
        [HttpPost("google-token")]
        public async Task<ActionResult<GoogleTokenResponseDto>> GoogleToken(GoogleTokenRequestDto dto)
        {
            var clientId = _configuration["GoogleAuth:ClientId"];
            var clientSecret = _configuration["GoogleAuth:ClientSecret"];

            var parameters = new Dictionary<string, string>
            {
                { "code", dto.Code },
                { "client_id", clientId ?? string.Empty },
                { "client_secret", clientSecret ?? string.Empty },
                { "redirect_uri", dto.RedirectUri },
                { "grant_type", "authorization_code" }
            };

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(parameters));

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Google token exchange failed.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<GoogleTokenResponseDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(token);
        }

        [AllowAnonymous]
        [HttpPost("google-signin")]
        public async Task<ActionResult<AuthResponseDto>> GoogleSignIn(GoogleLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }
            catch (Exception)
            {
                return Unauthorized("Invalid Google token.");
            }

            var email = payload.Email ?? $"{Guid.NewGuid():N}@example.com";
            var user = await _context.Users
                                     .Include(u => u.UserProfile)
                                     .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            var profilePic = !string.IsNullOrEmpty(dto.ProfilePicture)
                ? dto.ProfilePicture
                : !string.IsNullOrEmpty(payload.Picture)
                    ? payload.Picture
                    : "https://avatar.iran.liara.run/public/boy?username=Ash";
            var fullName = !string.IsNullOrEmpty(dto.FullName)
                ? dto.FullName
                : !string.IsNullOrEmpty(payload.Name)
                    ? payload.Name
                    : null;

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    PhoneNumber = GenerateRandomPhoneNumber(),
                    UserType = "Rider",
                    AccountStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                var randomPassword = Guid.NewGuid().ToString("N");
                user.PasswordHash = _passwordHasher.HashPassword(user, randomPassword);

                user.UserProfile = new UserProfile
                {
                    FullName = !string.IsNullOrEmpty(fullName) ? fullName : GenerateRandomName(),
                    ProfilePicture = profilePic,
                    Bio = "Default bio message for Google users.",
                    EmergencyContact = GenerateRandomPhoneNumber()
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (user.UserProfile != null)
            {
                var updated = false;
                if (string.IsNullOrEmpty(user.UserProfile.ProfilePicture))
                {
                    user.UserProfile.ProfilePicture = profilePic;
                    updated = true;
                }
                if (string.IsNullOrEmpty(user.UserProfile.FullName) && !string.IsNullOrEmpty(fullName))
                {
                    user.UserProfile.FullName = fullName;
                    updated = true;
                }
                if (updated)
                {
                    await _context.SaveChangesAsync();
                }
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
            var user = await _context.Users
                                     .Include(u => u.UserProfile)
                                     .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.UserProfile == null)
            {
                return NotFound();
            }

            var profileDto = new UserProfileDto
            {
                Id = user.UserProfile.Id,
                UserId = user.UserProfile.UserId,
                FullName = user.UserProfile.FullName,
                ProfilePicture = user.UserProfile.ProfilePicture,
                EmergencyContact = user.UserProfile.EmergencyContact,
                Bio = user.UserProfile.Bio,
                PhoneNumber = user.PhoneNumber
            };

            return Ok(profileDto);
        }

        [AllowAnonymous]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromQuery] int userId, UserProfileUpdateDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound();
            }

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
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

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

        private static string GenerateRandomPhoneNumber()
        {
            var random = new Random();
            var firstDigit = random.Next(6, 10);
            var rest = random.Next(100000000, 1000000000);
            return firstDigit.ToString() + rest.ToString();
        }

        private static string GenerateRandomName()
        {
            return $"User{Guid.NewGuid():N}".Substring(0, 10);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RideShare_Connect.DTOs;
using RideShare_Connect.Models.UserManagement;
using RideShareConnect.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RideShare_Connect.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserAccountService> _logger;

        private record PendingRegistration(UserRegisterDto User, string Otp);

        public UserAccountService(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration,
            IDistributedCache cache,
            IEmailService emailService,
            ILogger<UserAccountService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _cache = cache;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<RegistrationOtpResponseDto> SendVerificationOtpAsync(UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                throw new ServiceException("User with this email already exists.");
            }

            dto.UserType = string.IsNullOrWhiteSpace(dto.UserType) ? "Rider" : dto.UserType;

            var otp = new Random().Next(100000, 999999).ToString();
            var registrationId = Guid.NewGuid().ToString();
            var cacheKey = $"Registration-{registrationId}";
            var pending = new PendingRegistration(dto, otp);
            var json = JsonSerializer.Serialize(pending);

            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await _emailService.SendOtpEmailAsync(dto.Email, dto.FullName ?? "New User", otp, "Registration");

            return new RegistrationOtpResponseDto
            {
                Id = registrationId,
                Message = "An OTP has been sent to your email for verification."
            };
        }

        public async Task RegisterUserAsync(UserRegisterVerifyDto verificationDto)
        {
            var cacheKey = $"Registration-{verificationDto.RegistrationId}";
            var registrationJson = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(registrationJson))
            {
                throw new ServiceException("Invalid or expired registration.");
            }

            var pending = JsonSerializer.Deserialize<PendingRegistration>(registrationJson);
            if (pending == null || pending.Otp != verificationDto.Otp)
            {
                throw new ServiceException("Invalid or expired OTP.");
            }

            var userRegisterDto = pending.User;

            if (await _context.Users.AnyAsync(u => u.Email == userRegisterDto.Email))
            {
                throw new ServiceException("User with this email already exists.");
            }

            var newUser = new User
            {
                Email = userRegisterDto.Email,
                PhoneNumber = userRegisterDto.PhoneNumber,
                UserType = userRegisterDto.UserType ?? "Rider",
                AccountStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, userRegisterDto.Password ?? string.Empty);
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
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var normalizedEmail = loginDto.Email?.Trim().ToLower();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new ServiceException("Invalid credentials.");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new ServiceException("Invalid credentials.");
            }

            var token = GenerateJwtToken(user.Id, user.Email, user.UserType ?? "Rider");

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture = user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };
        }

        public async Task<AuthResponseDto> GoogleSignInAsync(GoogleLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Google token.");
                throw new ServiceException("Invalid Google token.");
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

            var token = GenerateJwtToken(user.Id, user.Email, user.UserType ?? "Rider");

            return new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture = user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                return;
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var cacheKey = $"PasswordReset-Otp-{user.Email}";

            await _cache.SetStringAsync(
                cacheKey,
                otp,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            await _emailService.SendOtpEmailAsync(user.Email, user.UserProfile?.FullName ?? "User", otp, "PasswordReset");
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var cacheKey = $"PasswordReset-Otp-{dto.Email}";
            var storedOtp = await _cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Otp)
            {
                throw new ServiceException("Invalid or expired OTP.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                throw new ServiceException("User not found.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword ?? string.Empty);
            await _cache.RemoveAsync(cacheKey);
            await _context.SaveChangesAsync();
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.UserProfile == null)
            {
                throw new ServiceException("Profile not found.");
            }

            return new UserProfileDto
            {
                Id = user.UserProfile.Id,
                UserId = user.UserProfile.UserId,
                FullName = user.UserProfile.FullName,
                ProfilePicture = user.UserProfile.ProfilePicture,
                EmergencyContact = user.UserProfile.EmergencyContact,
                Bio = user.UserProfile.Bio,
                PhoneNumber = user.PhoneNumber
            };
        }

        public async Task UpdateProfileAsync(int userId, UserProfileUpdateDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ServiceException("User not found.");
            }

            var profile = user.UserProfile ?? new UserProfile { UserId = userId };

            if (dto.FullName != null) profile.FullName = dto.FullName;
            if (dto.ProfilePicture != null) profile.ProfilePicture = dto.ProfilePicture;
            if (dto.EmergencyContact != null) profile.EmergencyContact = dto.EmergencyContact;
            if (dto.Bio != null) profile.Bio = dto.Bio;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

            if (user.UserProfile == null)
            {
                _context.UserProfiles.Add(profile);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(int userId, DeleteAccountDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ServiceException("User not found.");
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password ?? string.Empty);
            if (verify == PasswordVerificationResult.Failed)
            {
                throw new ServiceException("Invalid password.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
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
    }
}

// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
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

        public UsersController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _configuration = configuration; // Initialize IConfiguration
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
            var user = await _context.Users
                                     .Include(u => u.UserProfile) // Include UserProfile for FullName
                                     .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
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

            var responseDto = new AuthResponseDto
            {
                Token = tokenString,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.UserProfile?.FullName,
                ProfilePicture=user.UserProfile?.ProfilePicture,
                UserType = user.UserType
            };

            return Ok(responseDto);
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

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
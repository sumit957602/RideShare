using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using RideShareConnect.Data;
using RideShare_Connect.Models.UserManagement;
using RideShare_Connect.DTOs;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public DocumentController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadDto dto)
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

            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
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
                Status = "Pending",
                IssuedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddYears(1)
            };
            _context.UserVerifications.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { record.Id, record.DocUrl, record.VerificationType });
        }
    }
}

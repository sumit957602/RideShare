using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShareConnect.Data;
using RideShare_Connect.DTOs;
using System.Security.Claims;
using System.Linq;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VerificationController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyVerifications()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            var userId = int.Parse(userIdClaim);
            var records = await _context.UserVerifications
                .Where(v => v.UserId == userId)
                .Select(v => new { v.Id, v.DocUrl, v.VerificationType, v.Status, v.IssuedOn, v.ExpiresOn })
                .ToListAsync();
            return Ok(records);
        }

        // Ideally restricted to admins
        [Authorize]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, VerificationStatusUpdateDto dto)
        {
            var verification = await _context.UserVerifications.FindAsync(id);
            if (verification == null) return NotFound();

            verification.Status = dto.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

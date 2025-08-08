using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.Api.DTOs;
using System.Security.Claims;

namespace RideShare_Connect_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VerificationController(ApplicationDbContext context)
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

        // In a real application this would be restricted to admins
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

// Controllers/RatingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.Api.Data;
using RideShare_Connect.DTOs;
using RideShare_Connect_Backend.DTOs;
using RideShare_Connect_Backend.Services;
using System;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require auth to create ratings
public class RatingsController : ControllerBase
{
    private readonly IRatingService _service;
    private readonly ApplicationDbContext _db;

    public RatingsController(IRatingService service, ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DriverRatingCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        // Get passengerId from claims; ensure it's an int (adapt if you use GUID ids)
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idStr)) return Unauthorized("User not identified.");

        if (!int.TryParse(idStr, out var passengerId))
            return Unauthorized("Invalid user id in token.");

        try
        {
            var created = await _service.CreateAsync(passengerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await _db.DriverRatings.FindAsync(id);
        if (r == null) return NotFound();

        return Ok(new DriverRatingResponseDto
        {
            Id = r.Id,
            RideId = r.RideId,
            DriverId = r.DriverId,
            PassengerId = r.PassengerId,
            Rating = r.Rating,
            Review = r.Review,
            Timestamp = r.Timestamp
        });
    }

    // List ratings for a driver (paged)
    [HttpGet("driver/{driverId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverRatings(int driverId, int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize is < 1 or > 50) pageSize = 10;

        var query = _db.DriverRatings
            .Where(x => x.DriverId == driverId)
            .OrderByDescending(x => x.Timestamp);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DriverRatingResponseDto
            {
                Id = x.Id,
                RideId = x.RideId,
                DriverId = x.DriverId,
                PassengerId = x.PassengerId,
                Rating = x.Rating,
                Review = x.Review,
                Timestamp = x.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // Quick summary for driver (average + count)
    [HttpGet("driver/{driverId:int}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDriverSummary(int driverId)
    {
        var exists = await _db.Driver.AnyAsync(d => d.DriverId == driverId);
        if (!exists) return NotFound();

        var avg = await _db.DriverRatings
            .Where(x => x.DriverId == driverId)
            .Select(x => (double?)x.Rating)
            .AverageAsync();

        var count = await _db.DriverRatings
            .CountAsync(x => x.DriverId == driverId);

        return Ok(new { average = Math.Round(avg ?? 0, 2), count });
    }
}

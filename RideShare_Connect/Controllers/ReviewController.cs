using brevo_csharp.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideShare_Connect.DTOs;
using RideShare_Connect.ViewModels;
using RideShareConnect.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
public class ReviewController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<ReviewController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Review(int rideId, int driverId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "UserAccount");

        var ride = await _db.Rides
            .Include(r => r.Driver)
            .FirstOrDefaultAsync(r => r.Id == rideId && r.DriverId == driverId);

        if (ride == null) return NotFound();

        var vm = new SubmitDriverRatingVm
        {
            RideId = rideId,
            DriverId = driverId,
            DriverName = ride.Driver.FullName,
            From = ride.Origin,
            To = ride.Destination,
            DepartureTime = ride.DepartureTime
        };

        return View("Review", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(SubmitDriverRatingVm vm)
    {
        if (!ModelState.IsValid)
            return View("Review", vm);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "UserAccount");

        var dto = new DriverRatingCreateDto
        {
            RideId = vm.RideId,
            DriverId = vm.DriverId,
            Rating = vm.Rating,
            Review = vm.Review?.Trim()
        };

        var client = _httpClientFactory.CreateClient("RideShareApi");
        var token = HttpContext.Session.GetString("jwt");
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        var res = await client.PostAsJsonAsync("/api/ratings", dto);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation("POST /api/ratings returned {Status}: {Body}", res.StatusCode,body);

        if (res.IsSuccessStatusCode)
        {
            TempData["Toast"] = "Thanks for your review!";
            return RedirectToAction("User", "UserDashboard"); 
        }

        var err = await res.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Failed to submit review: {err}");
        return View("Review", vm);
    }
}

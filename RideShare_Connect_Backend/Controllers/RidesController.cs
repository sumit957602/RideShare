using Microsoft.AspNetCore.Mvc;
using RideShare_Connect.DTOs;
using RideShare_Connect.Services;

namespace RideShare_Connect.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RidesController : ControllerBase
    {
        private readonly RideService _rideService;

        public RidesController(RideService rideService)
        {
            _rideService = rideService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRide(RideCreateDto dto)
        {
            var ride = await _rideService.CreateRideAsync(dto);
            return CreatedAtAction(nameof(GetRide), new { id = ride.Id }, ride);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRide(int id)
        {
            var ride = await _rideService.GetRideAsync(id);
            if (ride == null) return NotFound();
            return Ok(ride);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditRide(int id, RideUpdateDto dto)
        {
            var ride = await _rideService.EditRideAsync(id, dto);
            if (ride == null) return NotFound();
            return Ok(ride);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelRide(int id)
        {
            var success = await _rideService.CancelRideAsync(id);
            if (!success) return BadRequest();
            return NoContent();
        }
    }
}

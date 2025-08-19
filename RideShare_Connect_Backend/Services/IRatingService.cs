using RideShare_Connect.DTOs;
using RideShare_Connect_Backend.DTOs;

namespace RideShare_Connect_Backend.Services
{
    public interface IRatingService
    {
        Task<DriverRatingResponseDto> CreateAsync(int passengerId, DriverRatingCreateDto dto);

    }
}

using RideShare_Connect.DTOs;

namespace RideShare_Connect.Services
{
    public interface IRideBookingService
    {
        Task<RideBookingResultDto> BookRideAsync(RideBookingRequestDto dto);
    }
}

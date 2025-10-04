using RideShare_Connect.DTOs;

namespace RideShare_Connect.Services
{
    public interface IAdminAccountService
    {
        Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto);
    }
}

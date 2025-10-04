using RideShare_Connect.DTOs;
using RideShare_Connect.Models.VehicleManagement;

namespace RideShare_Connect.Services
{
    public interface IDriverAccountService
    {
        Task RegisterAsync(DriverRegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<Vehicle> RegisterVehicleAsync(int driverId, VehicleRegisterDto dto);
    }
}

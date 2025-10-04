using RideShare_Connect.DTOs;

namespace RideShare_Connect.Services
{
    public interface IUserAccountService
    {
        Task RegisterUserAsync(UserRegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> GoogleSignInAsync(GoogleLoginDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<UserProfileDto> GetProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UserProfileUpdateDto dto);
        Task DeleteAccountAsync(int userId, DeleteAccountDto dto);
    }
}

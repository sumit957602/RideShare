namespace RideShare_Connect_Backend.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string toName);
        Task SendOtpEmailAsync(string toEmail, string toName, string otp, string purpose);

    }
}

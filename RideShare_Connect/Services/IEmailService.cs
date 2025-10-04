namespace RideShare_Connect.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string toName);
        Task SendOtpEmailAsync(string toEmail, string toName, string otp, string purpose);
    }
}

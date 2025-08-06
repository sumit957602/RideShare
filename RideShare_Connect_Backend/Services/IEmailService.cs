namespace RideShare_Connect_Backend.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string toName);
        Task SendVerificationEmailAsync(string toEmail, string token);
    }
}

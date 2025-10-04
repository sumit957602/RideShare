using Microsoft.Extensions.Logging;

namespace RideShare_Connect.Services
{
    public class LoggingEmailService : IEmailService
    {
        private readonly ILogger<LoggingEmailService> _logger;

        public LoggingEmailService(ILogger<LoggingEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendWelcomeEmailAsync(string toEmail, string toName)
        {
            _logger.LogInformation("Simulated welcome email sent to {Email} for {Name}.", toEmail, toName);
            return Task.CompletedTask;
        }

        public Task SendOtpEmailAsync(string toEmail, string toName, string otp, string purpose)
        {
            _logger.LogInformation("Simulated OTP email for {Purpose} sent to {Email} ({Name}) with OTP {Otp}.", purpose, toEmail, toName, otp);
            return Task.CompletedTask;
        }
    }
}

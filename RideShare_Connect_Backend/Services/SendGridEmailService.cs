using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
namespace RideShare_Connect_Backend.Services
{
    public class SendGridEmailService:IEmailService
    {
        private readonly IConfiguration _config;

        public SendGridEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string toName)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(_config["SendGrid:SenderEmail"], _config["SendGrid:SenderName"]);
            var to = new EmailAddress(toEmail, toName);
            var subject = "Welcome to Ride share App";
            var htmlContent = $"<h2>Hello {toName},</h2><h3>Welcome! We're happy to have you.</h3><p>Team RideShare</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlContent);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                throw new Exception($"SendGrid failed with status: {response.StatusCode}");
            }
        }
        public async Task SendOtpEmailAsync(string toEmail, string toName, string otp, string purpose)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(_config["SendGrid:SenderEmail"], _config["SendGrid:SenderName"]);
            var to = new EmailAddress(toEmail, toName);

            string subject;
            string htmlContent;

            if (purpose == "Registration")
            {
                subject = "Verify Your RideShare Account - OTP";
                htmlContent = $"<h2>Hello {toName},</h2><p>Thank you for registering with RideShare! To complete your registration, please use the following one-time password (OTP):</p><h3><strong>{otp}</strong></h3><p>This code is valid for 5 minutes. Do not share it with anyone.</p><p>Team RideShare</p>";
            }
            else if (purpose == "PasswordReset")
            {
                subject = "RideShare Password Reset OTP";
                htmlContent = $"<h2>Hello {toName},</h2><p>You recently requested to reset your password. Please use the following one-time password (OTP) to continue:</p><h3><strong>{otp}</strong></h3><p>This code is valid for 5 minutes. If you did not request this, please ignore this email.</p><p>Team RideShare</p>";
            }
            else
            {
                // Fallback for other purposes or if purpose is not recognized
                subject = "Your One-Time Password (OTP)";
                htmlContent = $"<h2>Hello {toName},</h2><p>Here is your OTP: <h3><strong>{otp}</strong></h3></p><p>Team RideShare</p>";
            }

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlContent);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                // In a real application, you'd log this error.
                throw new Exception($"SendGrid failed to send OTP email with status: {response.StatusCode}");
            }
        }
    }
}

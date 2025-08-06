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
    }
}

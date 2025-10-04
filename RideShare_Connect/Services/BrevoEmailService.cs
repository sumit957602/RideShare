using Microsoft.Extensions.Configuration;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Collections.Generic;

namespace RideShare_Connect.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly TransactionalEmailsApi _transactionalEmailsApi;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public BrevoEmailService(IConfiguration config)
        {
            var apiKey = config["Brevo:ApiKey"] ?? config["SendGrid:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Brevo API key is missing or empty. Please configure 'Brevo:ApiKey'.");
            }

            _senderEmail = config["Brevo:SenderEmail"] ?? config["SendGrid:SenderEmail"]
                ?? throw new InvalidOperationException("Sender email is missing. Configure 'Brevo:SenderEmail'.");
            _senderName = config["Brevo:SenderName"] ?? config["SendGrid:SenderName"] ?? _senderEmail;

            var brevoConfiguration = new Configuration();
            brevoConfiguration.ApiKey.Add("api-key", apiKey);

            _transactionalEmailsApi = new TransactionalEmailsApi(brevoConfiguration);
        }

        public Task SendWelcomeEmailAsync(string toEmail, string toName)
        {
            var subject = "Welcome to Ride share App";
            var htmlContent = $"<h2>Hello {toName},</h2><h3>Welcome! We're happy to have you.</h3><p>Team RideShare</p>";

            return SendEmailAsync(toEmail, toName, subject, htmlContent);
        }

        public Task SendOtpEmailAsync(string toEmail, string toName, string otp, string purpose)
        {
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
                subject = "Your One-Time Password (OTP)";
                htmlContent = $"<h2>Hello {toName},</h2><p>Here is your OTP:</p><h3><strong>{otp}</strong></h3><p>Team RideShare</p>";
            }

            return SendEmailAsync(toEmail, toName, subject, htmlContent);
        }

        private async Task SendEmailAsync(string toEmail, string? toName, string subject, string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));
            }

            var sendRequest = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(email: _senderEmail, name: _senderName),
                to: new List<SendSmtpEmailTo>
                {
                    new SendSmtpEmailTo(email: toEmail, name: toName)
                },
                subject: subject,
                htmlContent: htmlContent
            );

            try
            {
                await _transactionalEmailsApi.SendTransacEmailAsync(sendRequest);
            }
            catch (ApiException ex)
            {
                throw new InvalidOperationException($"Brevo failed to send email. Status: {ex.ErrorCode}.", ex);
            }
        }
    }
}

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Flight_Booking_System.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "no-reply@flightbooking.com";
        public string FromName { get; set; } = "Flight Booking";
    }

    public interface IAppEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }

    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            // No SMTP server configured (typical while developing): just log the email
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogWarning("SMTP is not configured. Email NOT sent. To: {To} | Subject: {Subject} | Body: {Body}",
                    toEmail, subject, htmlBody);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}

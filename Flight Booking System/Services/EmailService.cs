using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
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
        private readonly IHostEnvironment _env;

        public SmtpEmailSender(
            IOptions<EmailSettings> options,
            ILogger<SmtpEmailSender> logger,
            IHostEnvironment env)
        {
            _settings = options.Value;
            _logger = logger;
            _env = env;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                if (!_env.IsDevelopment())
                    throw new InvalidOperationException("SMTP is not configured.");

                // While developing there is no mail server, so we save the email
                // as an html file. Open it in the browser and click the button.
                var folder = Path.Combine(_env.ContentRootPath, "SentEmails");
                Directory.CreateDirectory(folder);

                var safeName = string.Concat(toEmail.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
                var path = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}.html");

                await File.WriteAllTextAsync(path, htmlBody);
                _logger.LogWarning("SMTP is not configured. The email to {To} was saved to {Path}", toEmail, path);
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

    // The html of the emails we send
    public static class EmailTemplates
    {
        public const string BrandName = "Flight Booking";

        public static string WithButton(string name, string message, string buttonText, string link)
        {
            var safeName = HtmlEncoder.Default.Encode(name);
            var safeMessage = HtmlEncoder.Default.Encode(message);
            var safeButton = HtmlEncoder.Default.Encode(buttonText);
            var safeLink = HtmlEncoder.Default.Encode(link);

            return
                "<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #e5e5e5;border-radius:8px\">" +
                $"<h2 style=\"color:#2c7be5;margin-top:0\">{BrandName}</h2>" +
                $"<p>Hi {safeName},</p>" +
                $"<p>{safeMessage}</p>" +
                $"<p style=\"margin:28px 0\"><a href=\"{safeLink}\" style=\"background:#2c7be5;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none\">{safeButton}</a></p>" +
                "<p style=\"font-size:12px;color:#777\">If the button does not work, copy this link into your browser:<br />" +
                $"{safeLink}</p>" +
                "<p style=\"font-size:12px;color:#777\">If you didn't ask for this email, you can ignore it.</p>" +
                "</div>";
        }
    }
}

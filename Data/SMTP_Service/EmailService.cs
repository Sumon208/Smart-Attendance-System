using System.Net;
using System.Net.Mail;

namespace Smart_Attendance_System.Data.SMTP_Service
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mail = new MailMessage(smtpUser, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    }
}

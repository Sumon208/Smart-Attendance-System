using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Smart_Attendance_System.Data.SMTP_Service
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly SmtpClient _client;
        private readonly string _from;

        public EmailService(IConfiguration config)
        {
            _config = config;

            var section = _config.GetSection("Smtp");
            var host = section.GetValue<string>("Host")!;
            var port = section.GetValue<int>("Port");
            var user = section.GetValue<string>("User")!;
            var pass = section.GetValue<string>("Pass")!;
            _from = section.GetValue<string>("From")!;
            var enableSsl = section.GetValue<bool>("EnableSsl");

            _client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl
            };
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var msg = new MailMessage(_from, to)
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            // You can add a plain-text alt view if you want:
            // var plain = AlternateView.CreateAlternateViewFromString(
            //     Regex.Replace(htmlBody, "<.*?>", string.Empty), null, "text/plain");
            // msg.AlternateViews.Add(plain);

            await _client.SendMailAsync(msg);
        }
    }
}

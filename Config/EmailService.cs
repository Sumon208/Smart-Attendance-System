using Microsoft.Extensions.Options;
using Smart_Attendance_System.Config;
using Smart_Attendance_System.EmailSettings;
using Smart_Attendance_System.Models;
using System.Net.Mail;
using System.Net;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using (var client = new SmtpClient(_settings.SmtpServer, _settings.Port))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            client.EnableSsl = _settings.EnableSsl;

            var mail = new MailMessage()
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }

    public async Task SendStatusUpdateAsync(string toEmail, string employeeName, EmployeeStatus status)
    {
        var subject = $"Employment Status Update - {status}";
        var body = $@"
            <p>Hello <b>{employeeName}</b>,</p>
            <p>Your employment status has been updated to: 
               <span style='color:blue;font-weight:bold'>{status}</span>.
            </p>
            <p>Regards,<br/>Admin Team</p>";

        await SendEmailAsync(toEmail, subject, body);
    }
}

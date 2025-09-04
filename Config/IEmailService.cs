using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.EmailSettings
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendStatusUpdateAsync(string toEmail, string employeeName, EmployeeStatus status);
    }

}

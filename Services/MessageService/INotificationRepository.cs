using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Services.MessageService
{
    public interface INotificationRepository
    {

        Task AddNotificationAsync(Notification notification);
        Task<IEnumerable<Notification>> GetNotificationsForAdminAsync();
        Task<IEnumerable<Notification>> GetNotificationsForEmployeeAsync(int employeeId);
        Task<int> GetUnreadCountForAdminAsync();
        Task<int> GetUnreadCountForEmployeeAsync(int employeeId);
        Task MarkAsReadAsync(int notificationId);

    }
}

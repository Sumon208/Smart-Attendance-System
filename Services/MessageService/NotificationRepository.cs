using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.MessageService;

namespace Smart_Attendance_System.Services.Repositores
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;
        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Notification>> GetNotificationsForAdminAsync()
        {
            return await _context.Notifications
                .Where(n => n.ForRole == "Admin")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }


        public async Task<IEnumerable<Notification>> GetNotificationsForEmployeeAsync(int employeeId)
        {
            return await _context.Notifications
                .Where(n => n.EmployeeId == employeeId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountForAdminAsync()
        {
            return await _context.Notifications.CountAsync(n => n.ForRole == "Admin" && !n.IsRead);
        }

        public async Task<int> GetUnreadCountForEmployeeAsync(int employeeId)
        {
            return await _context.Notifications.CountAsync(n => n.EmployeeId == employeeId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notif = await _context.Notifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
        
    }
}

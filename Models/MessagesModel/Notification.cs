using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int? EmployeeId { get; set; } // null means it's for Admins
        public string? ForRole { get; set; } // e.g., "Admin"

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

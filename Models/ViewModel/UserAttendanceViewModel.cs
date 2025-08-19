using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class UserAttendanceViewModel
    {
        public int EmployeeId { get; set; }
        public bool IsCheckedIn { get; set; }
        public bool IsCheckedOut { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public double? WorkingHours { get; set; }
        public bool IsLate { get; set; }
        public string Status { get; set; }
        public IEnumerable<Attendance> RecentAttendance { get; set; } = new List<Attendance>();
        
        // Additional properties for enhanced functionality
        public DateTime CurrentDate { get; set; } = DateTime.Today;
        public TimeSpan? OfficeStartTime { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM
        public TimeSpan? OfficeEndTime { get; set; } = new TimeSpan(17, 0, 0); // 5:00 PM
        public int TotalWorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AverageWorkingHours { get; set; }
    }
}

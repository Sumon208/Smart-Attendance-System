namespace Smart_Attendance_System.Models.ViewModel
{
    public class EmployeeDashboardVM
    {
        public string EmployeeName { get; set; } = string.Empty;
        public int Presents { get; set; }
        public int Absents { get; set; }
        public int LateArrivals { get; set; }
        public int LeavePending { get; set; }
        public int LeaveApproved { get; set; }
        public int LeaveRejected { get; set; }
        public double AttendanceRate { get; set; }
        public bool IsCheckedIn { get; set; }
        public DateTime? LastCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }
    }
}

using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class MonthlyAttendanceReportViewModel
    {
        public IEnumerable<Attendance> Attendances { get; set; } = new List<Attendance>();
        public string EmployeeSearch { get; set; } = "";
        public string DateFrom { get; set; } = "";
        public string DateTo { get; set; } = "";
        public int TotalRecords { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public double AttendanceRate => TotalRecords > 0 ? Math.Round((double)(PresentCount + LateCount) / TotalRecords * 100, 1) : 0;
    }
}

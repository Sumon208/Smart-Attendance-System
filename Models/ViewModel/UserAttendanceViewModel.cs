using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class UserAttendanceViewModel
    {
        public int SelectedEmployeeId { get; set; }
        public string AttendanceStatus { get; set; }
        public SelectList Employees { get; set; }
    }
}

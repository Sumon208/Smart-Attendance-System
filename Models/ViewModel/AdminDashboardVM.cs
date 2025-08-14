namespace Smart_Attendance_System.Models.ViewModel
{
    public class AdminDashboardVM
    {
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int LeavePending { get; set; }
        public int LeaveApproved { get; set; }
        public int LeaveRejected { get; set; }
        public decimal MonthlySalary { get; set; }
    }
}
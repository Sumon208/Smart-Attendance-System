namespace Smart_Attendance_System.Models.ViewModel
{
    public class MonthlySalaryViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;

        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public int? PresentCount { get; set; }
        public int? LateCount { get; set; }
        public int? AbsentCount { get; set; }
        public int? ApprovedLeaveDays { get; set; }
        public int? WorkingDays { get; set; }


        // Month/Year for identification
        public DateTime SalaryMonth { get; set; }
        // Audit
    }
}

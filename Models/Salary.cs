using System;

namespace Smart_Attendance_System.Models
{
    public class Salary
    {
        public int Id { get; set; }

        // Always required
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;

        // Nullable calculated fields
        public decimal? GrossSalary { get; set; }
        public decimal? NetSalary { get; set; }
        public int? PresentCount { get; set; }
        public int? LateCount { get; set; }
        public int? AbsentCount { get; set; }
        public int? ApprovedLeaveDays { get; set; }
        public int? WorkingDays { get; set; }

        public string? CountedLeaveIds { get; set; }
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Pending;

        // Month/Year as single DateTime
        public DateTime SalaryMonth { get; set; }
        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}

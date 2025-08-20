using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Smart_Attendance_System.Models
{
    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Leave
    {
        [Key]
        public int LeaveId { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        [BindNever]
        public Employee? Employee { get; set; }

        public string LeaveType { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public string Reason { get; set; }

        public LeaveStatus Status { get; set; } // Modified to use the enum

    }
}
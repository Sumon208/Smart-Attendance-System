using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Attendance_System.Models
{
    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public class EmployeeTask
    {
        [Key]
        public int TaskId { get; set; }

        [ForeignKey("Employee")]
        public int? EmployeeId { get; set; } // must be int to match Employee.Id
        public Employee? Employee { get; set; } // optional navigation

        [MaxLength(500)]
        public string? Project { get; set; }

        [MaxLength(100)]
        public string? Shift { get; set; }

        [MaxLength(1000)]
        public string? TodaysActivity { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public DateTime? DueDate { get; set; }

        public DateTime SubmitDate { get; set; } = DateTime.Now;
    }
}

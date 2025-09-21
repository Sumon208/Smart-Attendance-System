using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        [DataType(DataType.Time)]
        public DateTime? CheckInTime { get; set; }

        [DataType(DataType.Time)]
        public DateTime? CheckOutTime { get; set; }

        public double? WorkingHours { get; set; }

        public required string Status { get; set; }
    }
}

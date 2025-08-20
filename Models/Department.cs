using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models
{
    public class Department
    {
        [Key]
public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public required string DepartmentName { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}

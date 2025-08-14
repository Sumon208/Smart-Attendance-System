using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Attendance_System.Models
{
    public class SystemUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }

        [Required]
        public int UserType { get; set; }

        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeRecordId")]
        public Employee? Employee { get; set; }
    }
}

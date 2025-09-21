using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models
{
    public class EnumValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string EnumType { get; set; } 

        [Required]
        public string Name { get; set; } 
    }
}

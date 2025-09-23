using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class EmployeeTaskViewModel
    {
        [Key]
        public int TaskId { get; set; }

        [ForeignKey("Employee")]
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        // FK to EnumValue table
        [Display(Name = "Project")]
        public int? ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public EnumValue? Project { get; set; }

        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }
        [ForeignKey("ShiftId")]
        public EnumValue? Shift { get; set; }

        [Display(Name = "Status")]
        public int? StatusId { get; set; }
        [ForeignKey("StatusId")]
        public EnumValue? Status { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Today's Activity")]
        public string? TodaysActivity { get; set; }

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Submit Date")]
        public DateTime SubmitDate { get; set; } = DateTime.Now;
        //
        public List<SelectListItem> ShiftList { get; set; }
        public List<SelectListItem> ProjectList { get; set; }
        public List<SelectListItem> StatusList { get; set; }
    }
}

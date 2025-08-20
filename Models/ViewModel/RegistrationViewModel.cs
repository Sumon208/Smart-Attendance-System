using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class RegistrationViewModel
    {
        [Required]
        [Display(Name = "Employee Name")]
        public required string EmployeeName { get; set; }

        [Required]
        [Display(Name = "Employee ID")]
        public required string EmployeeId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }
    }
}
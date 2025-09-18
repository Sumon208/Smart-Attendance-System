using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}

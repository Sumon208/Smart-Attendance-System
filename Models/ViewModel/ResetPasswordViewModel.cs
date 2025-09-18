using System.ComponentModel.DataAnnotations;

namespace Smart_Attendance_System.Models.ViewModel
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required, DataType(DataType.Password), Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}

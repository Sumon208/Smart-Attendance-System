namespace Smart_Attendance_System.Models.ViewModel
{
    public class EmployeeAppointmentVM
    {
        public int EmployeeRecordId { get; set; }
        public required string EmployeeName { get; set; }
        public required string EmployeeId { get; set; }
        public required string Email { get; set; }
        public required string DepartmentName { get; set; }
    }
}

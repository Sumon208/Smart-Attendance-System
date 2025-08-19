using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetPendingEmployeesAsync();
        Task<IEnumerable<Employee>> GetApprovedEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int employeeId);
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId);
        Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);
        Task AddEmployeeAsync(Employee employee);
        
        // Attendance methods
        Task<Attendance?> GetTodayAttendanceAsync(int employeeId);
        Task<Attendance> CreateAttendanceAsync(Attendance attendance);
        Task UpdateAttendanceAsync(Attendance attendance);
        Task<IEnumerable<Attendance>> GetEmployeeAttendanceHistoryAsync(int employeeId, int days = 30);
        Task<bool> IsEmployeeCheckedInTodayAsync(int employeeId);
        Task<bool> IsEmployeeCheckedOutTodayAsync(int employeeId);
    }
}
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
    }
}
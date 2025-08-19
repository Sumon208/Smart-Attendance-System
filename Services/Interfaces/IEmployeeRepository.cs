using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<Employee>> GetPendingEmployeesAsync();
        Task<IEnumerable<Employee>> GetApprovedEmployeesAsync();
      
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId);
        Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee> GetEmployeeByIdAsync(int id);
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);
        Task AddEmployeeAsync(Employee employee);
    }
}
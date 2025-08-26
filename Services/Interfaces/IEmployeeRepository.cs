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
       
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);
        
        
        // Leave methods
        Task<Leave> CreateLeaveAsync(Leave leave);
        Task<IEnumerable<Leave>> GetEmployeeLeaveHistoryAsync(int employeeId);
        Task<Leave?> GetLeaveByIdAsync(int leaveId);
        Task UpdateLeaveAsync(Leave leave);
        Task DeleteLeaveAsync(int leaveId);
        Task<Employee?> GetEmployeeByIdAsync(int id);


        Task<int> GetEmployeeLeaveBalanceAsync(int employeeId, string leaveType);
        Task<bool> UpdateEmployeeAsyn(Employee employee);
    }
}
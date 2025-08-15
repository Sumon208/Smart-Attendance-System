using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IAdminRepository
    {
        Task<AdminDashboardVM> GetAdminDashboardDataAsync();
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int employeeId);

        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task AddDepartmentAsync(Department department);

        Task<IEnumerable<Employee>> GetPendingEmployeesAsync();
        Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status);

        Task<IEnumerable<Leave>> GetAllLeaveApplicationsAsync();
        Task UpdateLeaveStatusAsync(int leaveId, LeaveStatus status);

        Task<IEnumerable<Attendance>> GetEmployeeAttendanceAsync(int employeeId);
        Task<IEnumerable<Attendance>> GetAllAttendanceAsync();
    }
}
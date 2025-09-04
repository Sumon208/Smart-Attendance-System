using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IAdminRepository
    {
        // Dashboard
        Task<AdminDashboardVM> GetAdminDashboardDataAsync();

        // Employees
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeCode);
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int id);
        Task DeleteEmployeeWithRelatedDataAsync(int employeeId);

        // Departments
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task AddDepartmentAsync(Department department);

        // Employee Approval Workflow
       
        //Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status);
        //Task<bool> SetEmployeeStatusAsync(int employeeId, EmployeeStatus status);

        // Leaves
        Task<IEnumerable<Leave>> GetAllLeaveApplicationsAsync();
        Task<Leave> GetLeaveByIdAsync(int leaveId);
        Task UpdateLeaveStatusAsync(int leaveId, LeaveStatus status);
        Task<Leave?> GetLeaveIdAsync(int leaveId);

        // Attendance
        Task<IEnumerable<Attendance>> GetEmployeeAttendanceAsync(int employeeId);
        Task<IEnumerable<Attendance>> GetAllAttendanceAsync();
        Task<IEnumerable<Attendance>> GetAttendanceByDateAsync(DateTime date);

        // Employee Info for Views
        Task<IEnumerable<EmployeeVM>> GetAllEmployeeBasicInfoAsync();
        Task<EmployeeVM> GetEmployeeByIdByAsync(int employeeId);

        // Monthly Salary
        Task<List<MonthlySalaryViewModel>> GetMonthlySalaryReportAsync(DateTime? fromDate, DateTime? toDate);
        Task<MonthlySalaryViewModel?> GetMonthlySalaryByEmployeeIdAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);
        Task UpdateMonthlySalaryAsync(MonthlySalaryViewModel model);
        // for smtp gmail
        Task<IEnumerable<Employee>> GetPendingEmployeesAsync();
        Task<bool> SetEmployeeStatusAsync(int employeeId, EmployeeStatus status);
        Task<string?> GetUserEmailForEmployeeAsync(int employeeId);
        Task<Employee?> GetEmployeeIdAsync(int employeeId);
        Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status);
    }
}

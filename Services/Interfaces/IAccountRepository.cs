using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IAccountRepository
    {
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<SystemUser?> GetUserByEmailAsync(string email);
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId);
        Task<bool> RegisterUserAsync(Employee employee, SystemUser systemUser);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();

        Task<SystemUser?> GetUserByEmployeeIdAsync(int employeeRecordId);
        Task<SystemUser?> GetUserByEmailWithEmployeeAsync(string email);
        Task<bool> InitializeEmployeeSalaryAsync(Employee employee);


        // for reset password functionalities
        Task SavePasswordResetTokenAsync(int userId, string token, DateTime expiry);
        Task<PasswordResetToken?> GetValidResetTokenAsync(string token);
        Task InvalidateResetTokenAsync(string token);
        Task UpdateUserPasswordAsync(int userId, string newPasswordHash);
    }
}
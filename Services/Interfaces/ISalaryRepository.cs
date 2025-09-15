using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface ISalaryRepository
    {
        // Get salary report for all employees in a given month
        Task<List<MonthlySalaryViewModel>> GetMonthlySalaryReportAsync(DateTime month);

        // Get salary for a single employee for a given month
        Task<MonthlySalaryViewModel?> GetMonthlySalaryByEmployeeIdAsync(int employeeId, DateTime month);

        // Save or update a salary record
        Task SaveMonthlySalaryAsync(MonthlySalaryViewModel model);

        // Delete a salary record for a given employee and month
        Task<bool> DeleteMonthlySalaryAsync(int employeeId, DateTime month);

        // Get saved salary history for a specific employee
        Task<List<Salary>> GetSalaryHistoryByEmployeeIdAsync(int employeeId);

        Task UpdateEmployeeMonthlySalaryAsync(int employeeId, DateTime salaryMonth);
    }
}

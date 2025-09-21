using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<Attendance?> GetTodayAttendanceAsync(int employeeId);
        Task<Attendance> CreateAttendanceAsync(Attendance attendance);
        Task UpdateAttendanceAsync(Attendance attendance);
        Task<IEnumerable<Attendance>> GetEmployeeAttendanceHistoryAsync(int employeeId, int days = 30);
        Task<bool> IsEmployeeCheckedInTodayAsync(int employeeId);
        Task<bool> IsEmployeeCheckedOutTodayAsync(int employeeId);
        Task<Employee?> GetEmployeeByIdAsync(int id);
       
        Task<IEnumerable<Attendance>> GetMonthlyAttendanceReportAsync(string? employeeSearch = null, DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<bool> HasApprovedLeaveTodayAsync(int employeeId);

    }


}

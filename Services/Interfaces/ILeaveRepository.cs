using Smart_Attendance_System.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface ILeaveRepository
    {
        Task AddLeaveAsync(Leave leave);
        Task UpdateLeaveAsync(Leave leave);
        Task DeleteLeaveAsync(int leaveId);
        Task<Leave?> GetLeaveByIdAsync(int leaveId);
        Task<IEnumerable<Leave>> GetLeavesByEmployeeIdAsync(int employeeId);
        Task<IEnumerable<Leave>> GetAllLeavesAsync();
    }
}

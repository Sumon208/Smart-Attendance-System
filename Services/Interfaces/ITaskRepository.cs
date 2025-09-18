using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Services.Interfaces
{
    public interface ITaskRepository
    {
        Task<List<EmployeeTask>> GetAllTasksAsync();
        Task<EmployeeTask?> GetTaskByIdAsync(int taskId);
        Task AddTaskAsync(EmployeeTask task);
        Task UpdateTaskAsync(EmployeeTask task);
        Task DeleteTaskAsync(int taskId);
        Task<List<EmployeeTask>> GetTasksByEmployeeAsync(int employeeId);

    }
}

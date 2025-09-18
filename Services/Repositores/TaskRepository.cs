using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeTask>> GetAllTasksAsync()
        {
            return await _context.EmployeeTasks
                                 .Include(t => t.Employee)
                                 .ToListAsync();
        }

        public async Task<EmployeeTask?> GetTaskByIdAsync(int taskId)
        {
            return await _context.EmployeeTasks
                                 .Include(t => t.Employee)
                                 .FirstOrDefaultAsync(t => t.TaskId == taskId);
        }

        public async Task AddTaskAsync(EmployeeTask task)
        {
            await _context.EmployeeTasks.AddAsync(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTaskAsync(EmployeeTask task)
        {
            _context.EmployeeTasks.Update(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            var task = await _context.EmployeeTasks.FindAsync(taskId);
            if (task != null)
            {
                _context.EmployeeTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<EmployeeTask>> GetTasksByEmployeeAsync(int employeeId)
        {
            return await _context.EmployeeTasks
                                 .Where(t => t.EmployeeId == employeeId)
                                 .Include(t => t.Employee)
                                 .ToListAsync();
        }
    }
}

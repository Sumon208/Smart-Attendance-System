using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Attendance_System.Services.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // In TaskRepository.cs

        public async Task<List<EmployeeTask>> GetAllTasksAsync()
        {
            // Eagerly load all required navigation properties
            return await _context.EmployeeTasks
                                 .Include(t => t.Employee)
                                 .Include(t => t.Project) // <-- Add this line
                                 .Include(t => t.Shift)   // <-- Add this line
                                 .Include(t => t.Status)  // <-- Add this line
                                 .ToListAsync();
        }

        public async Task<EmployeeTask?> GetTaskByIdAsync(int taskId)
        {
            return await _context.EmployeeTasks
                                 .Include(t => t.Employee)
                                 .FirstOrDefaultAsync(t => t.TaskId == taskId);
        }

        public async Task AddTaskAsync(EmployeeTaskViewModel model)
        {
            var entity = new EmployeeTask
            {
                TaskId = model.TaskId,
                EmployeeId = model.EmployeeId,
                ProjectId = model.ProjectId,
                ShiftId = model.ShiftId,
                StatusId = model.StatusId,
                TodaysActivity = model.TodaysActivity,
                DueDate = model.DueDate,
                SubmitDate = model.SubmitDate
            };

            await _context.EmployeeTasks.AddAsync(entity);
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

        // Fetch Enum values dynamically from EnumValue table
        public async Task<List<EnumValue>> GetEnumValuesByTypeAsync(string enumType)
        {
            return await _context.Enum
                                 .Where(e => e.EnumType == enumType)
                                 .ToListAsync();
        }
    }
}

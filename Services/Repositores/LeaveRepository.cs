using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Attendance_System.Services.Repositores
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly ApplicationDbContext _context;
        public LeaveRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddLeaveAsync(Leave leave)
        {
            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateLeaveAsync(Leave leave)
        {
            _context.Leaves.Update(leave);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteLeaveAsync(int leaveId)
        {
            var leave = await _context.Leaves.FindAsync(leaveId);
            if (leave != null)
            {
                _context.Leaves.Remove(leave);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Leave?> GetLeaveByIdAsync(int leaveId)
        {
            return await _context.Leaves.Include(l => l.Employee).FirstOrDefaultAsync(l => l.LeaveId == leaveId);
        }
        public async Task<IEnumerable<Leave>> GetLeavesByEmployeeIdAsync(int employeeId)
        {
            return await _context.Leaves.Where(l => l.EmployeeId == employeeId).OrderByDescending(l => l.StartDate).ToListAsync();
        }
        public async Task<IEnumerable<Leave>> GetAllLeavesAsync()
        {
            return await _context.Leaves.Include(l => l.Employee).OrderByDescending(l => l.StartDate).ToListAsync();
        }
    }
}

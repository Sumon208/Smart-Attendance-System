using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositores
{
    public class AttendanceRepository:IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;
        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        // Attendance methods implementation
        public async Task<Attendance?> GetTodayAttendanceAsync(int employeeId)
        {
            var today = DateTime.Today;
            return await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
        }

        public async Task<Attendance> CreateAttendanceAsync(Attendance attendance)
        {
            await _context.Attendances.AddAsync(attendance);
            await _context.SaveChangesAsync();
            return attendance;
        }

        public async Task UpdateAttendanceAsync(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Attendance>> GetEmployeeAttendanceHistoryAsync(int employeeId, int days = 30)
        {
            var startDate = DateTime.Today.AddDays(-days);
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.AttendanceDate >= startDate)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<bool> IsEmployeeCheckedInTodayAsync(int employeeId)
        {
            var today = DateTime.Today;
            return await _context.Attendances
                .AnyAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today && a.CheckInTime.HasValue);
        }

        public async Task<bool> IsEmployeeCheckedOutTodayAsync(int employeeId)
        {
            var today = DateTime.Today;
            return await _context.Attendances
                .AnyAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today && a.CheckOutTime.HasValue);
        }
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }

        // Monthly attendance report method
        public async Task<IEnumerable<Attendance>> GetMonthlyAttendanceReportAsync(string? employeeSearch = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
                .AsQueryable();

            // Filter by employee name or ID
            if (!string.IsNullOrEmpty(employeeSearch))
            {
                query = query.Where(a => 
                    a.Employee.EmployeeName.Contains(employeeSearch) || 
                    a.Employee.EmployeeId.Contains(employeeSearch));
            }

            // Filter by date range
            if (dateFrom.HasValue)
            {
                query = query.Where(a => a.AttendanceDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(a => a.AttendanceDate <= dateTo.Value);
            }

            // If no date filters, default to current month
            if (!dateFrom.HasValue && !dateTo.HasValue)
            {
                var currentMonth = DateTime.Today;
                var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                query = query.Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd);
            }

            return await query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee.EmployeeName)
                .ToListAsync();
        }
    }
}

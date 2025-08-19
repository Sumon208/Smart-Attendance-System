using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Employee>> GetPendingEmployeesAsync()
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .Where(e => e.Status == EmployeeStatus.Pending)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetApprovedEmployeesAsync()
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .Where(e => e.Status == EmployeeStatus.Approved)
                                 .ToListAsync();
        }

       
        public async Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId)
        {
            return await _context.Employees.SingleOrDefaultAsync(e => e.EmployeeId == employeeId);
        }

        public async Task UpdateEmployeeStatusAsync(int employeeId, EmployeeStatus status)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null)
            {
                employee.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees
                                 .Include(e => e.Department) // include department if navigation property
                                 .ToListAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
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
    }
}
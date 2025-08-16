using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardVM> GetAdminDashboardDataAsync()
        {
            var totalEmployees = await _context.Employees.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();
            var leavePending = await _context.Leaves.CountAsync(l => l.Status == LeaveStatus.Pending);
            var leaveApproved = await _context.Leaves.CountAsync(l => l.Status == LeaveStatus.Approved);
            var leaveRejected = await _context.Leaves.CountAsync(l => l.Status == LeaveStatus.Rejected);

            return new AdminDashboardVM
            {
                TotalEmployees = totalEmployees,
                TotalDepartments = totalDepartments,
                LeavePending = leavePending,
                LeaveApproved = leaveApproved,
                LeaveRejected = leaveRejected,
                MonthlySalary = 0.00m // Placeholder for future logic
            };
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.Include(e => e.Department).ToListAsync();
        }
        public async Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId)
        {
            return await _context.Employees.SingleOrDefaultAsync(e => e.EmployeeId == employeeId);
        }
        public async Task AddEmployeeAsync(Employee employee)
        {
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();
        }

        //public async Task UpdateEmployeeAsync(Employee employee)
        //{
        //    _context.Employees.Update(employee);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task DeleteEmployeeAsync(int employeeId)
        //{
        //    var employee = await _context.Employees.FindAsync(employeeId);
        //    if (employee != null)
        //    {
        //        _context.Employees.Remove(employee);
        //        await _context.SaveChangesAsync();
        //    }
        //}

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task AddDepartmentAsync(Department department)
        {
            await _context.Departments.AddAsync(department);
            await _context.SaveChangesAsync();
        }
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
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

        public async Task<IEnumerable<Leave>> GetAllLeaveApplicationsAsync()
        {
            return await _context.Leaves.Include(l => l.Employee).ToListAsync();
        }

        public async Task UpdateLeaveStatusAsync(int leaveId, LeaveStatus status)
        {
            var leave = await _context.Leaves.FindAsync(leaveId);
            if (leave != null)
            {
                leave.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Attendance>> GetEmployeeAttendanceAsync(int employeeId)
        {
            return await _context.Attendances
                               .Where(a => a.EmployeeId == employeeId)
                               .OrderByDescending(a => a.AttendanceDate)
                               .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAllAttendanceAsync()
        {
            return await _context.Attendances.Include(a => a.Employee).ToListAsync();
        }
    }
}
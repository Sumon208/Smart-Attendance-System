using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees.AsNoTracking().ToListAsync();
        }

        // Keep only this if you always need the Employee nav
        public async Task<SystemUser?> GetUserByEmailWithEmployeeAsync(string email)
        {
            return await _context.SystemUsers
                .AsNoTracking()
                .Include(u => u.Employee)
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        // Optional keep (without include) if used elsewhere
        public async Task<SystemUser?> GetUserByEmailAsync(string email)
        {
            return await _context.SystemUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<SystemUser?> GetUserByEmployeeIdAsync(int employeeRecordId)
        {
            return await _context.SystemUsers
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.EmployeeId == employeeRecordId);
        }

        public async Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeCode)
        {
            return await _context.Employees
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EmployeeId == employeeCode);
        }

        public async Task<bool> RegisterUserAsync(Employee employee, SystemUser systemUser)
        {
            using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                systemUser.EmployeeId = employee.Id;
                await _context.SystemUsers.AddAsync(systemUser);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.AsNoTracking().ToListAsync();
        }
    }
}

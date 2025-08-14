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
            return await _context.Employees.ToListAsync();
        }
        public async Task<SystemUser?> GetUserByEmailAsync(string email)
        {
            var result= await _context.SystemUsers
                                 .Include(u => u.Employee)
                                 .SingleOrDefaultAsync(u => u.Email == email);
            return result;
        }

        public async Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId)
        {
            return await _context.Employees.SingleOrDefaultAsync(e => e.EmployeeId == employeeId);
        }

        public async Task<bool> RegisterUserAsync(Employee employee, SystemUser systemUser)
        {
            using (IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Add the Employee to the database first.
                    //    The database will automatically generate a value for the 'Id' column.
                    await _context.Employees.AddAsync(employee);
                    await _context.SaveChangesAsync();

                    // 2. Link the SystemUser to the Employee using the newly generated Id.
                    systemUser.EmployeeId = employee.Id;
                    await _context.SystemUsers.AddAsync(systemUser);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }
    }
}
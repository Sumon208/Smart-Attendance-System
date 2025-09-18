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
        //public async Task<SystemUser?> GetUserByEmailAsync(string email)
        //{
        //    return await _context.SystemUsers
        //        .AsNoTracking()
        //        .SingleOrDefaultAsync(u => u.Email == email);
        //}

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
        public async Task<bool> InitializeEmployeeSalaryAsync(Employee employee)
        {
            using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var salary = new Salary
                {
                    EmployeeId = employee.Id,            // Primary Key from Employees table
                    EmployeeCode = employee.EmployeeId, // Employee code
                    EmployeeName = employee.EmployeeName,
                    Status = employee.Status,
                    GrossSalary = employee.Salary ?? 0, // default 0
                    NetSalary = 0,                       // default 0, will update later via attendance
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Salaries.AddAsync(salary);
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
        // for reset password functionality
        public async Task<SystemUser?> GetUserByEmailAsync(string email)
        {
            return await _context.SystemUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task SavePasswordResetTokenAsync(int userId, string token, DateTime expiry)
        {
            var resetToken = new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiryDate = expiry
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetToken?> GetValidResetTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow);
        }

        public async Task InvalidateResetTokenAsync(string token)
        {
            var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (resetToken != null)
            {
                _context.PasswordResetTokens.Remove(resetToken);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateUserPasswordAsync(int userId, string newPasswordHash)
        {
            var user = await _context.SystemUsers.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = newPasswordHash;
                await _context.SaveChangesAsync();
            }
        }

    }
}

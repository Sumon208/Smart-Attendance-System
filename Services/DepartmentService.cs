using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
        }

        public async Task<Department> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
        }

        public async Task<Department> GetDepartmentByNameAsync(string name)
        {
            return await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == name.ToLower());
        }

        public async Task<bool> AddDepartmentAsync(Department department)
        {
            try
            {
                // Check if department name already exists
                if (await IsDepartmentNameExistsAsync(department.DepartmentName))
                {
                    return false;
                }

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateDepartmentAsync(Department department)
        {
            try
            {
                var existingDept = await GetDepartmentByIdAsync(department.DepartmentId);
                if (existingDept == null)
                {
                    return false;
                }

                // Check if new name conflicts with existing departments (excluding current)
                var nameExists = await _context.Departments
                    .AnyAsync(d => d.DepartmentName.ToLower() == department.DepartmentName.ToLower() 
                                  && d.DepartmentId != department.DepartmentId);
                
                if (nameExists)
                {
                    return false;
                }

                existingDept.DepartmentName = department.DepartmentName;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            try
            {
                var department = await GetDepartmentByIdAsync(id);
                if (department == null)
                {
                    return false;
                }

                // Check if department has employees
                var hasEmployees = await _context.Employees
                    .AnyAsync(e => e.DepartmentId == id);

                if (hasEmployees)
                {
                    return false; // Cannot delete department with employees
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsDepartmentNameExistsAsync(string name)
        {
            return await _context.Departments
                .AnyAsync(d => d.DepartmentName.ToLower() == name.ToLower());
        }
    }
}



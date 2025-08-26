using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Smart_Attendance_System.Services.Repositores
{
    public class DepartmentRepository :IDepartmentRepository
    {
        private readonly ApplicationDbContext _context;
        public DepartmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Department>> GetDepartmentsAsync()
        {
            var result = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            return result;
        }
        public async Task<Department> GetDepartmentByIdASync(int id)
        {
            return await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
        }
         
        public async Task<bool> IsDepartmentNameExistsAsync(string name) 
        {
            return await _context.Departments.AnyAsync(d => d.DepartmentName.ToLower() == name.ToLower());
        }
        public async Task<bool> AddDepartmentAsync(Department department)
        {
            try
            {
                if (await IsDepartmentNameExistsAsync(department.DepartmentName))
                {
                    return false;
                }

                _context.Departments.Add(department);
                _context.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<bool> UpdateDepartmentAsync(Department department)
        {
            try
            {
                var exitsDept = await GetDepartmentByIdASync(department.DepartmentId);
                if (exitsDept == null)
                {
                    return false;

                }
                var newExits = await _context.Departments.AnyAsync(d => d.DepartmentName.ToLower() == department.DepartmentName.ToLower()
                 && d.DepartmentId != department.DepartmentId
                );
                if (newExits)
                {
                    return false;

                }
                exitsDept.DepartmentName = department.DepartmentName;
                _context.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> DeleteDepartmentASync(int id)
        {
            try
            {
                var dept = await GetDepartmentByIdASync(id);
                if (dept == null)
                {
                    return false;
                }
                var hasEmployee = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
                if (hasEmployee)
                {
                    return false;
                }
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}

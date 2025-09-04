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
            // delete all related SystemUsers first
            var systemUsers = _context.SystemUsers.Where(su => su.EmployeeId == id);
            _context.SystemUsers.RemoveRange(systemUsers);

            // then delete employee
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }




        // Leave methods implementation
        public async Task<Leave> CreateLeaveAsync(Leave leave)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CreateLeaveAsync called with: EmployeeId={leave.EmployeeId}, Type={leave.LeaveType}, Start={leave.StartDate}, End={leave.EndDate}, Reason={leave.Reason}");
                
                // Test database connection first
                if (!await _context.Database.CanConnectAsync())
                {
                    throw new InvalidOperationException("Cannot connect to database");
                }
                
                // Check if the database exists
                if (!await _context.Database.EnsureCreatedAsync())
                {
                    System.Diagnostics.Debug.WriteLine("Database creation attempted");
                }
                
                // Ensure the leave has a valid status
                if (leave.Status == 0) // Default enum value
                {
                    leave.Status = LeaveStatus.Pending;
                }
                
                // Validate required fields
                if (string.IsNullOrEmpty(leave.LeaveType))
                {
                    throw new ArgumentException("Leave type is required");
                }
                
                if (string.IsNullOrEmpty(leave.Reason))
                {
                    throw new ArgumentException("Reason is required");
                }
                
                if (leave.EmployeeId <= 0)
                {
                    throw new ArgumentException("Valid employee ID is required");
                }
                
                // Verify employee exists
                var employee = await _context.Employees.FindAsync(leave.EmployeeId);
                if (employee == null)
                {
                    throw new ArgumentException($"Employee with ID {leave.EmployeeId} not found");
                }
                
                System.Diagnostics.Debug.WriteLine("Validation passed, adding to context");
                
                // Add the leave to context
                _context.Leaves.Add(leave);
                
                System.Diagnostics.Debug.WriteLine("Leave added to context, saving changes");
                
                // Save changes with explicit transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var result = await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"SaveChanges result: {result} rows affected");
                    
                    if (result > 0)
                    {
                        // Commit the transaction
                        await transaction.CommitAsync();
                        
                        // Refresh the entity to get the generated ID
                        await _context.Entry(leave).ReloadAsync();
                        System.Diagnostics.Debug.WriteLine($"Leave saved successfully with ID: {leave.LeaveId}");
                        return leave;
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        throw new InvalidOperationException("Failed to save leave to database");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"Transaction failed: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error in CreateLeaveAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<IEnumerable<Leave>> GetEmployeeLeaveHistoryAsync(int employeeId)
        {
            return await _context.Leaves
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<Leave?> GetLeaveByIdAsync(int leaveId)
        {
            return await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveId == leaveId);
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

        public async Task<int> GetEmployeeLeaveBalanceAsync(int employeeId, string leaveType)
        {
            // Default leave balances (this could be moved to a configuration table)
            var defaultBalances = new Dictionary<string, int>
            {
                { "Annual", 15 },
                { "Sick", 10 },
                { "Personal", 5 },
                { "Maternity", 90 },
                { "Paternity", 10 },
                { "Other", 3 }
            };

            var defaultBalance = defaultBalances.ContainsKey(leaveType) ? defaultBalances[leaveType] : 0;
            
            // Get used leaves of this type
            var usedLeaves = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId && 
                           l.LeaveType == leaveType && 
                           l.Status == LeaveStatus.Approved)
                .SumAsync(l => (l.EndDate - l.StartDate).Days + 1);

            return Math.Max(0, defaultBalance - (int)usedLeaves);
        }
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }
        public async Task<bool> UpdateEmployeeAsyn(Employee employee)
        {
            try
            {
                var existingEmployee = await GetEmployeeByIdAsync(employee.Id);
                if (existingEmployee == null)
                    return false;

                // Check duplicate EmployeeId
                var isDuplicate = await _context.Employees
                    .AnyAsync(e => e.EmployeeId.ToLower() == employee.EmployeeId.ToLower()
                                && e.Id != employee.Id);
                if (isDuplicate)
                    return false;

                // Basic fields update
                existingEmployee.EmployeeId = employee.EmployeeId;
                existingEmployee.EmployeeName = employee.EmployeeName;
                existingEmployee.DateOfBirth = employee.DateOfBirth;
                existingEmployee.Gender = employee.Gender;
                existingEmployee.Email = employee.Email;
                existingEmployee.Address = employee.Address;
                existingEmployee.JoiningDate = employee.JoiningDate;
                existingEmployee.DepartmentId = employee.DepartmentId;
                existingEmployee.Salary = employee.Salary;
                existingEmployee.Nationality = employee.Nationality;
                existingEmployee.Description = employee.Description;
                existingEmployee.MobileNumber = employee.MobileNumber;
                existingEmployee.BloodGroup = employee.BloodGroup;

                // Photo upload
                if (employee.EmployeePhotoFile != null && employee.EmployeePhotoFile.Length > 0)
                {
                    var photoFileName = Guid.NewGuid() + Path.GetExtension(employee.EmployeePhotoFile.FileName);
                    var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/photos", photoFileName);

                    using (var stream = new FileStream(photoPath, FileMode.Create))
                    {
                        await employee.EmployeePhotoFile.CopyToAsync(stream);
                    }

                    existingEmployee.EmployeePhotoPath = "/uploads/photos/" + photoFileName;
                }

                // Certificate upload
                if (employee.CertificateFile != null && employee.CertificateFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(employee.CertificateFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                        return false;

                    var certFileName = Guid.NewGuid() + ext;
                    var certPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/certificates", certFileName);

                    using (var stream = new FileStream(certPath, FileMode.Create))
                    {
                        await employee.CertificateFile.CopyToAsync(stream);
                    }

                    existingEmployee.CertificateFilePath = "/uploads/certificates/" + certFileName;
                }
                else if (string.IsNullOrEmpty(existingEmployee.CertificateFilePath))
                {
                    // আগের certificate নেই এবং নতুনও নেই -> fail
                    return false;
                }
                // অন্যথায়: আগের certificate preserve হবে

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
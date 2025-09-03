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

        public async Task DeleteEmployeeWithRelatedDataAsync(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee != null && employee.Id > 0)
            {
                // Delete related SystemUsers first
                var relatedUsers = _context.SystemUsers.Where(u => u.EmployeeId == employee.Id);
                _context.SystemUsers.RemoveRange(relatedUsers);

                // Delete the employee
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Leave>> GetAllLeaveApplicationsAsync()
        {
            return await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
                .ToListAsync();
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

        // AdminRepository.cs

        public async Task<IEnumerable<Employee>> GetPendingEmployeesAsync()
        {
            return await _context.Employees
                                 .Include(e => e.Department)       
                                 .Where(e => e.Status == EmployeeStatus.Pending)
                                 .ToListAsync();
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
        public async Task<IEnumerable<Attendance>> GetAttendanceByDateAsync(DateTime date)
        {
            return await _context.Attendances
                                .Where(a => a.AttendanceDate.Date == date.Date)
                                .Include(a => a.Employee)
                                .ToListAsync();
        }
        public async Task<IEnumerable<EmployeeVM>> GetAllEmployeeBasicInfoAsync()
        {
            // Join Employees with SystemUsers to get email
            var result = await (from e in _context.Employees
                                join u in _context.SystemUsers on e.Id equals u.EmployeeId
                                select new EmployeeVM
                                {
                                    EmployeePhotoPath = e.EmployeePhotoPath,
                                    EmployeeName = e.EmployeeName,
                                    Email = u.Email
                                }).ToListAsync();

            return result;
        }
        public async Task<EmployeeVM> GetEmployeeByIdByAsync(int employeeId)
        {
            var result = await (from e in _context.Employees
                                join u in _context.SystemUsers on e.Id equals u.EmployeeId
                                where e.Id == employeeId
                                select new EmployeeVM
                                {
                                    EmployeePhotoPath = e.EmployeePhotoPath,
                                    EmployeeName = e.EmployeeName,
                                    Email = u.Email
                                }).FirstOrDefaultAsync();

            return result;
        }
        public async Task<Leave> GetLeaveByIdAsync(int leaveId)
        {
            return await _context.Leaves
                .Include(l => l.Employee) 
                .FirstOrDefaultAsync(l => l.LeaveId == leaveId);
        }

        // for notification
        // ✅ Get unread notifications
        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n =>
                    (n.EmployeeId == userId || n.ForRole == "Admin") // match employee OR admin
                    && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // ✅ Mark notification as read
        public async Task<bool> MarkNotificationAsReadAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return false;

            notification.IsRead = true;
           

            return await _context.SaveChangesAsync() > 0;
        }


        // MonthlySalaryReport
        public async Task<List<MonthlySalaryViewModel>> GetMonthlySalaryReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var employees = await _context.Employees
                .Include(e => e.Attendances)
                .Include(e => e.Leaves) // Include Leave table
                .ToListAsync();

            var result = new List<MonthlySalaryViewModel>();

            foreach (var emp in employees)
            {
                if (!emp.Salary.HasValue) continue; // skip if no salary

                // Filter attendances by date range
                var attendances = emp.Attendances.AsQueryable();
                if (fromDate.HasValue)
                    attendances = attendances.Where(a => a.AttendanceDate >= fromDate.Value);
                if (toDate.HasValue)
                    attendances = attendances.Where(a => a.AttendanceDate <= toDate.Value);

                var present = attendances.Count(a => a.Status == "Present");
                var late = attendances.Count(a => a.Status == "Late");
                var absent = attendances.Count(a => a.Status == "Absent");

                // Calculate approved leave days within date range
                var approvedLeaves = emp.Leaves
                    .Where(l => l.Status == LeaveStatus.Approved
                                && (!fromDate.HasValue || l.EndDate >= fromDate.Value)
                                && (!toDate.HasValue || l.StartDate <= toDate.Value))
                    .Sum(l =>
                    {
                        var leaveStart = fromDate.HasValue && l.StartDate < fromDate.Value ? fromDate.Value : l.StartDate;
                        var leaveEnd = toDate.HasValue && l.EndDate > toDate.Value ? toDate.Value : l.EndDate;

                        int leaveDays = 0;
                        for (DateTime d = leaveStart; d <= leaveEnd; d = d.AddDays(1))
                        {
                            if (d.DayOfWeek != DayOfWeek.Friday)
                                leaveDays++;
                        }
                        return leaveDays;
                    });

                // Gross salary from Employee table
                var grossSalary = emp.Salary.Value;

                // Calculate total working days in period (skip Fridays)
                DateTime start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime end = toDate ?? start.AddMonths(1).AddDays(-1);

                int workingDays = 0;
                for (DateTime d = start; d <= end; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Friday)
                        workingDays++;
                }
                if (workingDays == 0) workingDays = 26; // fallback default

                // Per-day salary
                var perDay = grossSalary / workingDays;

                // Monthly salary = Present + Late + ApprovedLeaveDays
                var monthlySalary = (present + late + approvedLeaves) * perDay;

                result.Add(new MonthlySalaryViewModel
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeId,
                    EmployeeName = emp.EmployeeName,
                    GrossSalary = grossSalary,
                    MonthlySalary = monthlySalary,   // accurate, not rounded
                    PresentCount = present,
                    LateCount = late,
                    AbsentCount = absent,
                    ApprovedLeaveDays = approvedLeaves // you can add this property in ViewModel
                });
            }

            return result;
        }

        

        public async Task<MonthlySalaryViewModel?> GetMonthlySalaryByEmployeeIdAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Reuse existing method to compute monthly report (keeps logic consistent)
            var all = await GetMonthlySalaryReportAsync(fromDate, toDate);
            return all.FirstOrDefault(x => x.EmployeeId == employeeId);
        }

        public async Task UpdateMonthlySalaryAsync(MonthlySalaryViewModel model)
        {
            if (model == null) return;

            // Find employee entity by PK (model.EmployeeId stores Employee.Id)
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null) throw new InvalidOperationException("Employee not found.");

            
            employee.Salary = model.GrossSalary;

            // If you have a separate salary history table, add a record here (optional)

            await _context.SaveChangesAsync();
        }




    }
}
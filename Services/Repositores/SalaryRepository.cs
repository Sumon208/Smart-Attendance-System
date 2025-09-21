using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositories
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly ApplicationDbContext _context;

        public SalaryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Helper: Get Govt holidays for Bangladesh dynamically
        private List<DateTime> GetGovtHolidays(int year, int month)
        {
            var holidays = new List<DateTime>
            {
                new DateTime(year, 1, 1),   // New Year
                new DateTime(year, 3, 26),  // Independence Day
                new DateTime(year, 5, 1),   // Labor Day
                new DateTime(year, 12, 16)  // Victory Day
            };

            // Filter only holidays for the specified month
            return holidays.Where(d => d.Month == month).ToList();
        }

        // ✅ Get salary report for all employees in a given month
        public async Task<List<MonthlySalaryViewModel>> GetMonthlySalaryReportAsync(DateTime month)
        {
            DateTime fromDate = new DateTime(month.Year, month.Month, 1);
            DateTime toDate = fromDate.AddMonths(1).AddDays(-1);

            var employees = await _context.Employees
                .Include(e => e.Attendances)
                .Include(e => e.Leaves)
                .ToListAsync();

            var holidays = GetGovtHolidays(month.Year, month.Month);

            var result = new List<MonthlySalaryViewModel>();

            foreach (var emp in employees)
            {
                if (!emp.Salary.HasValue) continue;

                // ✅ Attendance filter
                var attendances = emp.Attendances
                    .Where(a => a.AttendanceDate >= fromDate && a.AttendanceDate <= toDate)
                    .ToList();

                int present = attendances.Count(a => a.Status == "Present");
                int late = attendances.Count(a => a.Status == "Late");
                int absent = attendances.Count(a => a.Status == "Absent");

                // ✅ Approved leaves
                var approvedLeaves = emp.Leaves
                    .Where(l => l.Status == LeaveStatus.Approved &&
                               l.StartDate <= toDate &&
                               l.EndDate >= fromDate)
                    .Sum(l =>
                    {
                        DateTime leaveStart = l.StartDate < fromDate ? fromDate : l.StartDate;
                        DateTime leaveEnd = l.EndDate > toDate ? toDate : l.EndDate;

                        int days = 0;
                        for (DateTime d = leaveStart; d <= leaveEnd; d = d.AddDays(1))
                        {
                            if (d.DayOfWeek != DayOfWeek.Friday && !holidays.Contains(d.Date))
                                days++;
                        }
                        return days;
                    });

                // ✅ Gross Salary
                var grossSalary = emp.Salary.Value;

                // ✅ Calculate working days (excluding Fridays + Holidays)
                int workingDays = 0;
                for (DateTime d = fromDate; d <= toDate; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Friday && !holidays.Contains(d.Date))
                        workingDays++;
                }
                if (workingDays == 0) workingDays = 26;

                decimal perDaySalary = grossSalary / workingDays;

                // ✅ Net Salary = Attendance based
                decimal netSalary = (present + late + approvedLeaves) * perDaySalary;

                result.Add(new MonthlySalaryViewModel
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeId,
                    EmployeeName = emp.EmployeeName,
                    GrossSalary = grossSalary,
                    NetSalary = netSalary,
                    PresentCount = present,
                    LateCount = late,
                    AbsentCount = absent,
                    ApprovedLeaveDays = approvedLeaves,
                    WorkingDays = workingDays,
                    SalaryMonth = fromDate,  // ✅ Month as DateTime
                   
                });
            }

            return result;
        }

        // ✅ Get salary for one employee (check by month)
        public async Task<MonthlySalaryViewModel?> GetMonthlySalaryByEmployeeIdAsync(int employeeId, DateTime month)
        {
            var report = await GetMonthlySalaryReportAsync(month);
            return report.FirstOrDefault(x => x.EmployeeId == employeeId);
        }

        // ✅ Save or update salary record
        public async Task SaveMonthlySalaryAsync(MonthlySalaryViewModel model)
        {
            var existing = await _context.Salaries
                .FirstOrDefaultAsync(m => m.EmployeeId == model.EmployeeId &&
                                          m.SalaryMonth == model.SalaryMonth);

            if (existing == null)
            {
                var salary = new Salary
                {
                    EmployeeId = model.EmployeeId,
                    EmployeeCode = model.EmployeeCode,
                    EmployeeName = model.EmployeeName,
                    GrossSalary = model.GrossSalary,
                    NetSalary = model.NetSalary,
                    PresentCount = model.PresentCount,
                    LateCount = model.LateCount,
                    WorkingDays = model.WorkingDays,
                    AbsentCount = model.AbsentCount,
                    ApprovedLeaveDays = model.ApprovedLeaveDays,
                    SalaryMonth = model.SalaryMonth,   // ✅ DateTime
                   
                    CreatedAt = DateTime.Now
                };
                await _context.Salaries.AddAsync(salary);
            }
            else
            {
                existing.GrossSalary = model.GrossSalary;
                existing.NetSalary = model.NetSalary;
                existing.PresentCount = model.PresentCount;
                existing.LateCount = model.LateCount;
                existing.AbsentCount = model.AbsentCount;
                existing.ApprovedLeaveDays = model.ApprovedLeaveDays;
                existing.UpdatedAt = DateTime.Now;
                _context.Salaries.Update(existing);
            }

            await _context.SaveChangesAsync();
        }

        // ✅ Delete salary record
        public async Task<bool> DeleteMonthlySalaryAsync(int employeeId, DateTime month)
        {
            var record = await _context.Salaries
                .FirstOrDefaultAsync(m => m.EmployeeId == employeeId &&
                                          m.SalaryMonth == month);

            if (record == null) return false;

            _context.Salaries.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ View saved salary history
        public async Task<List<Salary>> GetSalaryHistoryByEmployeeIdAsync(int employeeId)
        {
            return await _context.Salaries
                .Where(m => m.EmployeeId == employeeId)
                .OrderByDescending(m => m.SalaryMonth)
                .ToListAsync();
        }

        public async Task UpdateEmployeeMonthlySalaryAsync(int employeeId, DateTime salaryMonth)
        {
            var employee = await _context.Employees
                .Include(e => e.Attendances)
                .Include(e => e.Leaves)
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.Salary.HasValue);

            if (employee == null) return;

            DateTime startDate = new DateTime(salaryMonth.Year, salaryMonth.Month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            var attendances = employee.Attendances
                .Where(a => a.AttendanceDate.Date >= startDate && a.AttendanceDate.Date <= endDate)
                .ToList();

            int present = attendances.Count(a => a.Status == "Present");
            int late = attendances.Count(a => a.Status == "Late");
            int absent = attendances.Count(a => a.Status == "Absent");

            // Approved leave days (excluding Fridays)
            int approvedLeaves = 0;
            foreach (var leave in employee.Leaves.Where(l => l.Status == LeaveStatus.Approved))
            {
                DateTime leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
                DateTime leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;

                for (DateTime d = leaveStart; d <= leaveEnd; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Friday)
                        approvedLeaves++;
                }
            }

            // ✅ Actual working days in office (Present + Late)
            int workingDaysActual = present + late;

            // ✅ Working days for salary calculation (total possible working days in month excluding Fridays)
            int workingDaysForSalary = Enumerable.Range(0, (int)(endDate - startDate).TotalDays + 1)
                                                 .Select(d => startDate.AddDays(d))
                                                 .Count(d => d.DayOfWeek != DayOfWeek.Friday);
            if (workingDaysForSalary == 0) workingDaysForSalary = 26;

            decimal perDay = employee.Salary.Value / workingDaysForSalary;
            decimal netSalary = (workingDaysActual + approvedLeaves) * perDay;

            var salaryRecord = await _context.Salaries
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId &&
                                          s.SalaryMonth.Year == salaryMonth.Year &&
                                          s.SalaryMonth.Month == salaryMonth.Month);

            if (salaryRecord == null)
            {
                // New row
                salaryRecord = new Salary
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    GrossSalary = employee.Salary.Value,
                    NetSalary = netSalary,
                    PresentCount = present,
                    LateCount = late,
                    AbsentCount = absent,
                    ApprovedLeaveDays = approvedLeaves,
                    WorkingDays = workingDaysActual, // store actual present + late
                    SalaryMonth = salaryMonth,
                    Status = EmployeeStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Salaries.Add(salaryRecord);
            }
            else
            {
                // Update existing row
                salaryRecord.NetSalary = netSalary;
                salaryRecord.PresentCount = present;
                salaryRecord.LateCount = late;
                salaryRecord.AbsentCount = absent;
                salaryRecord.ApprovedLeaveDays = approvedLeaves;
                salaryRecord.WorkingDays = workingDaysActual; // update actual present + late
                salaryRecord.UpdatedAt = DateTime.UtcNow;

                _context.Salaries.Update(salaryRecord);
            }

            await _context.SaveChangesAsync();
        }


    }
}

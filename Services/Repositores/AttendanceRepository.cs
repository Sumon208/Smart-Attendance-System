using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Services.Repositores
{
    public class AttendanceRepository:IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;
        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees
                                 .Include(e => e.Department)
                                 .FirstOrDefaultAsync(e => e.Id == id);
        }

         public async Task<IEnumerable<Attendance>> GetMonthlyAttendanceReportAsync(
                string? employeeSearch = null,
                DateTime? dateFrom = null,
                DateTime? dateTo = null)
            {
                // Step 1: Fetch employees first
                var employeesQuery = _context.Employees
                    .Include(e => e.Department)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(employeeSearch))
                {
                    employeesQuery = employeesQuery.Where(e =>
                        e.EmployeeName.Contains(employeeSearch) ||
                        e.EmployeeId.Contains(employeeSearch));
                }

                var employees = await employeesQuery.ToListAsync();

                // Step 2: Set default date range (current month)
                if (!dateFrom.HasValue && !dateTo.HasValue)
                {
                    var today = DateTime.Today;
                    dateFrom = new DateTime(today.Year, today.Month, 1);
                    dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                }

                if (dateFrom.HasValue && !dateTo.HasValue)
                    dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);

                if (!dateFrom.HasValue && dateTo.HasValue)
                    dateFrom = new DateTime(dateTo.Value.Year, dateTo.Value.Month, 1);

                // Step 3: Generate list of dates in range
                var dates = Enumerable.Range(0, (dateTo.Value - dateFrom.Value).Days + 1)
                                      .Select(offset => dateFrom.Value.AddDays(offset))
                                      .ToList();

                // Step 4: Fetch actual attendances from database for the date range
                var allAttendances = await _context.Attendances
                    .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(a => a.AttendanceDate >= dateFrom.Value && a.AttendanceDate <= dateTo.Value)
                    .ToListAsync();

                // Step 5: Generate report combining employees and dates
                var report = employees
                    .SelectMany(emp => dates, (emp, date) => new { emp, date })
                    .Select(ed =>
                    {
                        var att = allAttendances
                            .FirstOrDefault(a => a.EmployeeId == ed.emp.Id && a.AttendanceDate.Date == ed.date.Date);

                        return new Attendance
                        {
                            AttendanceId = att?.AttendanceId ?? 0,
                            Employee = ed.emp,
                            EmployeeId = ed.emp.Id,
                            AttendanceDate = ed.date,
                            CheckInTime = att?.CheckInTime,
                            CheckOutTime = att?.CheckOutTime,
                            Status = att == null
                                ? "Absent"
                                : (att.CheckInTime.HasValue && att.CheckInTime.Value.TimeOfDay > new TimeSpan(9, 15, 0))
                                    ? "Late"
                                    : "Present"
                        };
                    })
                    .OrderByDescending(a => a.AttendanceDate)
                    .ThenBy(a => a.Employee.EmployeeName)
                    .ToList();

                return report;
         }
        // Salary Related


        




    }
}

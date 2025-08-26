using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using System.Security.Claims;
using Smart_Attendance_System.Services.Repositores;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "2")] // Restrict access to only Employee users (UserType 2)
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendancerepository;
        private readonly IAccountRepository _accountRepository;

        public EmployeeController(IEmployeeRepository employeeRepository, IAccountRepository accountRepository,IAttendanceRepository attendanceRepository)
        {
            _employeeRepository = employeeRepository;
            _accountRepository = accountRepository;
            _attendancerepository = attendanceRepository;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Dashboard()
        {
            // Get current employee ID from claims
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);

            // Get attendance data for current month
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;
            var monthStart = new DateTime(currentYear, currentMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthlyAttendance = await _attendancerepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 31);
            var monthAttendance = monthlyAttendance
                .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd)
                .ToList();

            // Office working hours
            var officeStart = new TimeSpan(9, 0, 0);   // 9 AM
            var officeEnd = new TimeSpan(18, 0, 0);    // 6 PM

            // Calculate statistics
            var presents = monthAttendance.Count(a =>
                a.CheckInTime.HasValue &&
                a.CheckOutTime.HasValue &&
                a.CheckInTime.Value.TimeOfDay >= officeStart &&
                a.CheckOutTime.Value.TimeOfDay <= officeEnd
            );

            var absents = monthAttendance.Count(a =>
                !a.CheckInTime.HasValue ||
                !a.CheckOutTime.HasValue ||
                a.CheckInTime.Value.TimeOfDay < officeStart ||
                a.CheckOutTime.Value.TimeOfDay > officeEnd
            );

            var lateArrivals = monthAttendance.Count(a =>
                a.CheckInTime.HasValue && a.CheckInTime.Value.TimeOfDay > officeStart
            );

            // Calculate attendance rate
            var totalWorkingDays = monthAttendance.Count;
            var attendanceRate = totalWorkingDays > 0
                ? Math.Round((double)presents / totalWorkingDays * 100, 1)
                : 0;

            // Get today's attendance status
            var todayAttendance = await _attendancerepository.GetTodayAttendanceAsync(employeeIdInt);
            var isCheckedIn = todayAttendance?.CheckInTime.HasValue ?? false;

            // Get last check-in and check-out times
            var lastCheckIn = monthAttendance
                .Where(a => a.CheckInTime.HasValue)
                .OrderByDescending(a => a.AttendanceDate)
                .FirstOrDefault()?.CheckInTime;

            var lastCheckOut = monthAttendance
                .Where(a => a.CheckOutTime.HasValue)
                .OrderByDescending(a => a.AttendanceDate)
                .FirstOrDefault()?.CheckOutTime;

            // Get leave statistics for current year
            var currentYearStart = new DateTime(currentYear, 1, 1);
            var currentYearEnd = new DateTime(currentYear, 12, 31);

            var leaveHistory = await _employeeRepository.GetEmployeeLeaveHistoryAsync(employeeIdInt);
            var yearLeaveHistory = leaveHistory
                .Where(l => l.StartDate >= currentYearStart && l.StartDate <= currentYearEnd)
                .ToList();

            var leavePending = yearLeaveHistory.Count(l => l.Status == LeaveStatus.Pending);
            var leaveApproved = yearLeaveHistory.Count(l => l.Status == LeaveStatus.Approved);
            var leaveRejected = yearLeaveHistory.Count(l => l.Status == LeaveStatus.Rejected);

            var vm = new EmployeeDashboardVM
            {
                EmployeeName = User.Identity?.Name ?? "Unknown",
                Presents = presents,
                Absents = absents,
                LateArrivals = lateArrivals,
                LeavePending = leavePending,
                LeaveApproved = leaveApproved,
                LeaveRejected = leaveRejected,
                AttendanceRate = attendanceRate,
                IsCheckedIn = isCheckedIn,
                LastCheckIn = lastCheckIn,
                LastCheckOut = lastCheckOut
            };

            return View(vm);
        }



        public async Task<IActionResult> Profile()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get employee profile data
            var employee = await _attendancerepository.GetEmployeeByIdAsync(employeeIdInt);
            
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction("Dashboard");
            }

            return View(employee);
        }

        // Helper method to calculate working hours
        private double CalculateWorkingHours(DateTime? checkInTime, DateTime? checkOutTime)
        {
            if (!checkInTime.HasValue || !checkOutTime.HasValue)
                return 0;

            var duration = checkOutTime!.Value - checkInTime!.Value;
            return duration.TotalHours;
        }
        


    }
}

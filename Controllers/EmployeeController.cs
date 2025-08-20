using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using System.Security.Claims;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "2")] // Restrict access to only Employee users (UserType 2)
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAccountRepository _accountRepository;

        public EmployeeController(IEmployeeRepository employeeRepository, IAccountRepository accountRepository)
        {
            _employeeRepository = employeeRepository;
            _accountRepository = accountRepository;
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
            
            var monthlyAttendance = await _employeeRepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 31);
            var monthAttendance = monthlyAttendance.Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= monthEnd).ToList();
            
            // Calculate statistics
            var presents = monthAttendance.Count(a => a.CheckInTime.HasValue);
            var absents = monthAttendance.Count(a => !a.CheckInTime.HasValue);
            var lateArrivals = monthAttendance.Count(a => a.Status == "Late");
            
            // Calculate attendance rate based on actual data
            var totalWorkingDays = monthAttendance.Count;
            var attendanceRate = totalWorkingDays > 0 ? Math.Round((double)presents / totalWorkingDays * 100, 1) : 0;
            
            // Get today's attendance status
            var todayAttendance = await _employeeRepository.GetTodayAttendanceAsync(employeeIdInt);
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

            var vm = new EmployeeDashboardVM
            {
                EmployeeName = User.Identity?.Name,
                Presents = presents,
                Absents = absents,
                LateArrivals = lateArrivals,
                LeavePending = 1, // TODO: Implement leave counting
                LeaveApproved = 5, // TODO: Implement leave counting
                LeaveRejected = 0, // TODO: Implement leave counting
                AttendanceRate = attendanceRate,
                IsCheckedIn = isCheckedIn,
                LastCheckIn = lastCheckIn,
                LastCheckOut = lastCheckOut
            };

            return View(vm);
        }

        public async Task<IActionResult> Attendance()
        {
            // Get current employee ID from claims
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get today's attendance
            var todayAttendance = await _employeeRepository.GetTodayAttendanceAsync(employeeIdInt);
            
            // Get recent attendance history
            var recentAttendance = await _employeeRepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 7);
            
            var vm = new UserAttendanceViewModel
            {
                EmployeeId = employeeIdInt,
                IsCheckedIn = todayAttendance?.CheckInTime.HasValue ?? false,
                IsCheckedOut = todayAttendance?.CheckOutTime.HasValue ?? false,
                CheckInTime = todayAttendance?.CheckInTime,
                CheckOutTime = todayAttendance?.CheckOutTime,
                WorkingHours = CalculateWorkingHours(todayAttendance?.CheckInTime, todayAttendance?.CheckOutTime),
                IsLate = todayAttendance?.Status == "Late",
                Status = todayAttendance?.Status ?? "Normal",
                RecentAttendance = recentAttendance.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var employeeIdInt = int.Parse(employeeId);
                
                // Check if already checked in today
                var todayAttendance = await _employeeRepository.GetTodayAttendanceAsync(employeeIdInt);
                
                if (todayAttendance != null && todayAttendance.CheckInTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You have already checked in today.";
                    return RedirectToAction("Attendance");
                }

                var currentTime = DateTime.Now;
                var isLate = currentTime.TimeOfDay > new TimeSpan(9, 30, 0); // After 9:30 AM
                
                if (todayAttendance == null)
                {
                    // Create new attendance record
                    var newAttendance = new Attendance
                    {
                        EmployeeId = employeeIdInt,
                        AttendanceDate = DateTime.Today,
                        CheckInTime = currentTime,
                        Status = isLate ? "Late" : "Present"
                    };
                    
                    await _employeeRepository.CreateAttendanceAsync(newAttendance);
                }
                else
                {
                    // Update existing record
                    todayAttendance.CheckInTime = currentTime;
                    todayAttendance.Status = isLate ? "Late" : "Present";
                    await _employeeRepository.UpdateAttendanceAsync(todayAttendance);
                }
                
                var message = isLate ? "Check-in successful! You arrived late today." : "Check-in successful! Welcome to work.";
                TempData["SuccessMessage"] = message;
                return RedirectToAction("Attendance");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Check-in failed. Please try again.";
                return RedirectToAction("Attendance");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var employeeIdInt = int.Parse(employeeId);
                
                // Check if checked in today
                var todayAttendance = await _employeeRepository.GetTodayAttendanceAsync(employeeIdInt);
                
                if (todayAttendance == null || !todayAttendance.CheckInTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You must check in before checking out.";
                    return RedirectToAction("Attendance");
                }
                
                if (todayAttendance.CheckOutTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You have already checked out today.";
                    return RedirectToAction("Attendance");
                }

                var currentTime = DateTime.Now;
                todayAttendance.CheckOutTime = currentTime;
                
                // Calculate working hours
                var workingHours = (currentTime - todayAttendance.CheckInTime.Value).TotalHours;
                
                await _employeeRepository.UpdateAttendanceAsync(todayAttendance);
                
                TempData["SuccessMessage"] = $"Check-out successful! You worked for {workingHours:F1} hours today. Have a great day!";
                return RedirectToAction("Attendance");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Check-out failed. Please try again.";
                return RedirectToAction("Attendance");
            }
        }

        public async Task<IActionResult> LeaveApply()
        {
            return View(new Leave());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLeave(Leave leave)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                TempData["ErrorMessage"] = "Validation failed: " + errors;
                return View("LeaveApply", leave);
            }

            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 1. Set employee ID
                leave.EmployeeId = int.Parse(employeeId);
                // 2. Set status to Pending
                leave.Status = LeaveStatus.Pending;
                // 3. Save to database
                await _employeeRepository.AddLeaveAsync(leave);
                TempData["SuccessMessage"] = "Leave application submitted successfully! It will be reviewed by your manager.";
                return RedirectToAction("LeaveHistory");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Leave application failed. Please try again.";
                return View("LeaveApply", leave);
            }
        }

        public async Task<IActionResult> LeaveHistory()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }
            var leaves = await _employeeRepository.GetLeavesByEmployeeIdAsync(int.Parse(employeeId));
            return View(leaves);
        }

        public async Task<IActionResult> AttendanceHistory(int page = 1, int pageSize = 5, string status = "", string dateFrom = "", string dateTo = "")
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get all attendance history for filtering
            var allAttendance = await _employeeRepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 365);
            
            // Apply filters
            var filteredAttendance = allAttendance.AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredAttendance = filteredAttendance.Where(a => a.Status == status);
            }
            
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate >= fromDate);
            }
            
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate <= toDate);
            }
            
            // Calculate statistics
            var totalRecords = filteredAttendance.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            
            // Apply pagination
            var paginatedAttendance = filteredAttendance
                .OrderByDescending(a => a.AttendanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Calculate statistics for the filtered data
            var presentDays = filteredAttendance.Count(a => a.Status == "Present");
            var absentDays = filteredAttendance.Count(a => a.Status == "Absent");
            var lateDays = filteredAttendance.Count(a => a.Status == "Late");
            var attendanceRate = totalRecords > 0 ? Math.Round((double)presentDays / totalRecords * 100, 1) : 0;
            
            // Calculate average working hours
            var workingDays = filteredAttendance.Where(a => a.CheckInTime.HasValue && a.CheckOutTime.HasValue);
            var averageWorkingHours = workingDays.Any() ? workingDays.Average(a => (a.CheckOutTime.Value - a.CheckInTime.Value).TotalHours) : 0;
            
            // Create view model for pagination and filtering
            var viewModel = new AttendanceHistoryViewModel
            {
                Attendances = paginatedAttendance,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                StatusOptions = new List<string> { "All", "Present", "Late", "Absent" },
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                AttendanceRate = attendanceRate,
                AverageWorkingHours = Math.Round(averageWorkingHours, 1)
            };

            return View(viewModel);
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
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeIdInt);
            
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction("Dashboard");
            }

            return View(employee);
        }


        public async Task<IActionResult> EditLeave(int id)
        {
            var employeeId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var leave = (await _employeeRepository.GetLeavesByEmployeeIdAsync(int.Parse(employeeId))).FirstOrDefault(l => l.LeaveId == id);
            if (leave == null || leave.Status != LeaveStatus.Pending)
                return RedirectToAction("LeaveHistory");
            return View(leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeave(Leave leave)
        {
            var employeeId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            // Only allow update if status is Pending and belongs to this employee
            var existing = (await _employeeRepository.GetLeavesByEmployeeIdAsync(int.Parse(employeeId))).FirstOrDefault(l => l.LeaveId == leave.LeaveId);
            if (existing == null || existing.Status != LeaveStatus.Pending)
                return RedirectToAction("LeaveHistory");
            // Update allowed fields
            existing.LeaveType = leave.LeaveType;
            existing.StartDate = leave.StartDate;
            existing.EndDate = leave.EndDate;
            existing.Reason = leave.Reason;
            await _employeeRepository.UpdateLeaveAsync(existing);
            TempData["SuccessMessage"] = "Leave application updated successfully.";
            return RedirectToAction("LeaveHistory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLeave(int id)
        {
            var employeeId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var leave = (await _employeeRepository.GetLeavesByEmployeeIdAsync(int.Parse(employeeId))).FirstOrDefault(l => l.LeaveId == id);
            if (leave == null || leave.Status != LeaveStatus.Pending)
                return RedirectToAction("LeaveHistory");
            await _employeeRepository.DeleteLeaveAsync(id);
            TempData["SuccessMessage"] = "Leave application deleted successfully.";
            return RedirectToAction("LeaveHistory");

        // Helper method to calculate working hours
        private double CalculateWorkingHours(DateTime? checkInTime, DateTime? checkOutTime)
        {
            if (!checkInTime.HasValue || !checkOutTime.HasValue)
                return 0;

            var duration = checkOutTime.Value - checkInTime.Value;
            return duration.TotalHours;
        }
        
        // Export attendance data to CSV
        [HttpGet]
        public async Task<IActionResult> ExportAttendance(string status = "", string dateFrom = "", string dateTo = "")
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get filtered attendance data
            var allAttendance = await _employeeRepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 365);
            var filteredAttendance = allAttendance.AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredAttendance = filteredAttendance.Where(a => a.Status == status);
            }
            
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate >= fromDate);
            }
            
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate <= toDate);
            }
            
            var attendanceList = filteredAttendance.OrderByDescending(a => a.AttendanceDate).ToList();
            
            // Generate CSV content
            var csvContent = "Date,Day,Check In,Check Out,Working Hours,Status\n";
            
            foreach (var attendance in attendanceList)
            {
                var checkIn = attendance.CheckInTime?.ToString("HH:mm") ?? "—";
                var checkOut = attendance.CheckOutTime?.ToString("HH:mm") ?? "—";
                var workingHours = attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue 
                    ? (attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours.ToString("F1") 
                    : "—";
                
                csvContent += $"{attendance.AttendanceDate:yyyy-MM-dd}," +
                             $"{attendance.AttendanceDate:dddd}," +
                             $"{checkIn}," +
                             $"{checkOut}," +
                             $"{workingHours}," +
                             $"{attendance.Status}\n";
            }
            
            var fileName = $"attendance_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);

        }
    }
}

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

            // TODO: Implement actual data retrieval from repository
            var vm = new EmployeeDashboardVM
            {
                EmployeeName = User.Identity?.Name,
                Presents = 18,
                Absents = 2,
                LateArrivals = 3,
                LeavePending = 1,
                LeaveApproved = 5,
                LeaveRejected = 0,
                IsCheckedIn = false,
                LastCheckIn = DateTime.Today.AddHours(9).AddMinutes(5),
                LastCheckOut = DateTime.Today.AddHours(17).AddMinutes(30)
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

            // TODO: Implement actual attendance data retrieval
            var vm = new UserAttendanceViewModel
            {
                EmployeeId = int.Parse(employeeId),
                IsCheckedIn = false,
                IsCheckedOut = false,
                CheckInTime = null,
                CheckOutTime = null,
                WorkingHours = 0,
                IsLate = false,
                Status = "Normal",
                RecentAttendance = new List<Attendance>()
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
                // TODO: Implement actual check-in logic
                // 1. Check if already checked in today
                // 2. Create attendance record
                // 3. Update status
                
                TempData["SuccessMessage"] = "Check-in successful! Welcome to work.";
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
                // TODO: Implement actual check-out logic
                // 1. Check if checked in today
                // 2. Update attendance record with check-out time
                // 3. Calculate working hours
                
                TempData["SuccessMessage"] = "Check-out successful! Have a great day.";
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

        public async Task<IActionResult> AttendanceHistory()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            // TODO: Implement actual attendance history retrieval
            var attendance = new List<Attendance>();

            return View(attendance);
        }

        public async Task<IActionResult> Profile()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            // TODO: Implement actual profile retrieval
            var employee = new Employee();

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
        }
    }
}

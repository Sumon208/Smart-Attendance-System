using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;
using System.Security.Claims;
using Smart_Attendance_System.Services.MessageService;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Employee")] // Restrict access to only Employee users (UserType 2)
    public class LeaveController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly INotificationRepository _notificationRepository;

        public LeaveController(IEmployeeRepository employeeRepository,
                       INotificationRepository notificationRepository)
        {
            _employeeRepository = employeeRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> LeaveApply()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var employeeIdInt = int.Parse(employeeId);
                
                // Use static values for leave balances (temporary until database is implemented)
                // TODO: Replace with actual database calls when leave balance system is implemented
                var staticAnnualBalance = 21; // Standard annual leave days
                var staticSickBalance = 14;   // Standard sick leave days
                var staticPersonalBalance = 7; // Standard personal leave days
                
                // Get approved leaves to calculate actual available balance
                var approvedLeaves = await _employeeRepository.GetEmployeeLeaveHistoryAsync(employeeIdInt);
                var usedAnnualLeaves = approvedLeaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "annual")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                var usedSickLeaves = approvedLeaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "sick")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                var usedPersonalLeaves = approvedLeaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "personal")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                
                // Calculate actual available balance
                var annualBalance = staticAnnualBalance - usedAnnualLeaves;
                var sickBalance = staticSickBalance - usedSickLeaves;
                var personalBalance = staticPersonalBalance - usedPersonalLeaves;
                
                ViewBag.AnnualBalance = annualBalance;
                ViewBag.SickBalance = sickBalance;
                ViewBag.PersonalBalance = personalBalance;
                ViewBag.StaticAnnualBalance = staticAnnualBalance;
                ViewBag.StaticSickBalance = staticSickBalance;
                ViewBag.StaticPersonalBalance = staticPersonalBalance;
                
                // Get pending leaves count
                var pendingLeaves = await _employeeRepository.GetEmployeeLeaveHistoryAsync(employeeIdInt);
                ViewBag.PendingLeavesCount = pendingLeaves.Count(l => l.Status == LeaveStatus.Pending);
                
                // Get recent leave applications (last 5)
                var recentLeaves = pendingLeaves.Take(5).ToList();
                ViewBag.RecentLeaves = recentLeaves;
                
                return View(new Leave());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to load leave information.";
                return View(new Leave());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLeave(Leave leave)
        {
            if (!ModelState.IsValid)
            {
                return View("LeaveApply", leave);
            }

            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(employeeId))
            {
                TempData["ErrorMessage"] = "User not authenticated.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Validate dates
                if (leave.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
                    return View("LeaveApply", leave);
                }

                if (leave.EndDate < leave.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
                    return View("LeaveApply", leave);
                }

                // Ensure leave type is selected
                if (string.IsNullOrWhiteSpace(leave.LeaveType))
                {
                    ModelState.AddModelError("LeaveType", "Leave type is required.");
                    return View("LeaveApply", leave);
                }

                // Calculate leave duration
                var leaveDuration = (leave.EndDate - leave.StartDate).Days + 1;

                // Define static limits (later can move to DB)
                var staticLeaveBalance = leave.LeaveType.ToLower() switch
                {
                    "annual" => 21,
                    "sick" => 14,
                    "personal" => 7,
                    _ => 0
                };

                // Ensure repository is available
                if (_employeeRepository == null)
                {
                    TempData["ErrorMessage"] = "System error: Employee repository is not available.";
                    return View("LeaveApply", leave);
                }

                // Get approved leaves safely
                var approvedLeaves = await _employeeRepository
                    .GetEmployeeLeaveHistoryAsync(int.Parse(employeeId))
                    ?? new List<Leave>();

                var usedLeaves = approvedLeaves
                    .Where(l => l != null
                                && l.Status == LeaveStatus.Approved
                                && !string.IsNullOrWhiteSpace(l.LeaveType)
                                && l.LeaveType.ToLower() == leave.LeaveType.ToLower())
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);

                // Remaining balance
                var leaveBalance = staticLeaveBalance - usedLeaves;

                if (leaveBalance < leaveDuration)
                {
                    ModelState.AddModelError("",
                        $"Insufficient leave balance. You have {leaveBalance} days remaining for {leave.LeaveType} leave. (Total: {staticLeaveBalance}, Used: {usedLeaves})");
                    return View("LeaveApply", leave);
                }

                // Save leave request
                var newLeave = new Leave
                {
                    EmployeeId = int.Parse(employeeId),
                    LeaveType = leave.LeaveType,  // string from dropdown
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    Reason = leave.Reason,
                    Status = LeaveStatus.Pending
                };

                var savedLeave = await _employeeRepository.CreateLeaveAsync(newLeave);

                if (savedLeave == null || savedLeave.LeaveId <= 0)
                {
                    TempData["ErrorMessage"] = "Leave application failed to save. Please try again.";
                    return View("LeaveApply", leave);
                }

                if (_notificationRepository != null)
                {
                    await _notificationRepository.AddNotificationAsync(new Notification
                    {
                        ForRole = "Admin",
                        Title = "New Leave Application",
                        Message = $"Leave #{savedLeave.LeaveId} from {savedLeave.StartDate:dd MMM} to {savedLeave.EndDate:dd MMM}",
                        LinkUrl = Url.Action("Leave", "Admin", null, Request.Scheme)
                    });
                }



                TempData["SuccessMessage"] = "Leave application submitted successfully! It will be reviewed by your manager.";
                return RedirectToAction("LeaveHistory");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Leave application failed: {ex.Message}. Please try again.";
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

            try
            {
                var employeeIdInt = int.Parse(employeeId);
                var leaves = await _employeeRepository.GetEmployeeLeaveHistoryAsync(employeeIdInt);
                
                // Calculate leave statistics
                var totalLeaves = leaves.Count();
                var approvedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved);
                var pendingLeaves = leaves.Count(l => l.Status == LeaveStatus.Pending);
                var rejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected);
                
                // Calculate leave balance information
                var staticAnnualBalance = 21;
                var staticSickBalance = 14;
                var staticPersonalBalance = 7;
                
                var usedAnnualLeaves = leaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "annual")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                var usedSickLeaves = leaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "sick")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                var usedPersonalLeaves = leaves
                    .Where(l => l.Status == LeaveStatus.Approved && l.LeaveType.ToLower() == "personal")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);
                
                var availableAnnualBalance = staticAnnualBalance - usedAnnualLeaves;
                var availableSickBalance = staticSickBalance - usedSickLeaves;
                var availablePersonalBalance = staticPersonalBalance - usedPersonalLeaves;
                
                ViewBag.TotalLeaves = totalLeaves;
                ViewBag.ApprovedLeaves = approvedLeaves;
                ViewBag.PendingLeaves = pendingLeaves;
                ViewBag.RejectedLeaves = rejectedLeaves;
                
                // Leave balance information
                ViewBag.StaticAnnualBalance = staticAnnualBalance;
                ViewBag.StaticSickBalance = staticSickBalance;
                ViewBag.StaticPersonalBalance = staticPersonalBalance;
                ViewBag.UsedAnnualLeaves = usedAnnualLeaves;
                ViewBag.UsedSickLeaves = usedSickLeaves;
                ViewBag.UsedPersonalLeaves = usedPersonalLeaves;
                ViewBag.AvailableAnnualBalance = availableAnnualBalance;
                ViewBag.AvailableSickBalance = availableSickBalance;
                ViewBag.AvailablePersonalBalance = availablePersonalBalance;

                return View(leaves);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to retrieve leave history.";
                return View(new List<Leave>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelLeave(int leaveId)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var leave = await _employeeRepository.GetLeaveByIdAsync(leaveId);
                
                if (leave == null)
                {
                    TempData["ErrorMessage"] = "Leave request not found.";
                    return RedirectToAction("LeaveHistory");
                }

                // Check if the leave belongs to the current employee
                if (leave.EmployeeId != int.Parse(employeeId))
                {
                    TempData["ErrorMessage"] = "You can only cancel your own leave requests.";
                    return RedirectToAction("LeaveHistory");
                }

                // Check if the leave is still pending
                if (leave.Status != LeaveStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending leave requests can be cancelled.";
                    return RedirectToAction("LeaveHistory");
                }

                // Delete the leave request
                await _employeeRepository.DeleteLeaveAsync(leaveId);

                TempData["SuccessMessage"] = "Leave request cancelled successfully.";
                return RedirectToAction("LeaveHistory");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to cancel leave request.";
                return RedirectToAction("LeaveHistory");
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "2")]
    public class LeaveController : Controller
    {
        private readonly ILeaveRepository _leaveRepository;
        public LeaveController(ILeaveRepository leaveRepository)
        {
            _leaveRepository = leaveRepository;
        }

        public async Task<IActionResult> Apply()
        {
            return View(new Leave());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Leave leave)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                TempData["ErrorMessage"] = "Validation failed: " + errors;
                return View("Apply", leave);
            }
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            leave.EmployeeId = int.Parse(employeeId);
            leave.Status = LeaveStatus.Pending;
            await _leaveRepository.AddLeaveAsync(leave);
            TempData["SuccessMessage"] = "Leave application submitted successfully!";
            return RedirectToAction("History");
        }

        public async Task<IActionResult> History()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var leaves = await _leaveRepository.GetLeavesByEmployeeIdAsync(int.Parse(employeeId));
            return View(leaves);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var leave = await _leaveRepository.GetLeaveByIdAsync(id);
            if (leave == null || leave.EmployeeId != int.Parse(employeeId) || leave.Status != LeaveStatus.Pending)
                return RedirectToAction("History");
            return View(leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Leave leave)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var existing = await _leaveRepository.GetLeaveByIdAsync(leave.LeaveId);
            if (existing == null || existing.EmployeeId != int.Parse(employeeId) || existing.Status != LeaveStatus.Pending)
                return RedirectToAction("History");
            existing.LeaveType = leave.LeaveType;
            existing.StartDate = leave.StartDate;
            existing.EndDate = leave.EndDate;
            existing.Reason = leave.Reason;
            await _leaveRepository.UpdateLeaveAsync(existing);
            TempData["SuccessMessage"] = "Leave application updated successfully.";
            return RedirectToAction("History");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return RedirectToAction("Login", "Account");
            var leave = await _leaveRepository.GetLeaveByIdAsync(id);
            if (leave == null || leave.EmployeeId != int.Parse(employeeId) || leave.Status != LeaveStatus.Pending)
                return RedirectToAction("History");
            await _leaveRepository.DeleteLeaveAsync(id);
            TempData["SuccessMessage"] = "Leave application deleted successfully.";
            return RedirectToAction("History");
        }
    }
}

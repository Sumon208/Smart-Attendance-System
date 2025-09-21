using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalaryController : Controller
    {
        private readonly ISalaryRepository _salaryRepository;

        public SalaryController(ISalaryRepository salaryRepository)
        {
            _salaryRepository = salaryRepository;
        }

        // ✅ GET: Monthly Salary Report
        [HttpGet]
        public async Task<IActionResult> MonthlySalaryReport(string? monthYear = null)
        {
            DateTime monthDate = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out var parsedMonth))
                monthDate = parsedMonth;

            var salaryData = await _salaryRepository.GetMonthlySalaryReportAsync(monthDate);

            ViewBag.MonthYear = monthDate.ToString("yyyy-MM");

            return View(salaryData);
        }

        // ✅ GET: View Salary Details for an Employee
        [HttpGet]
        public async Task<IActionResult> ViewSalaryDetails(int employeeId, string? monthYear = null)
        {
            DateTime monthDate = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out var parsedMonth))
                monthDate = parsedMonth;

            var employeeSalary = await _salaryRepository.GetMonthlySalaryByEmployeeIdAsync(employeeId, monthDate);
            if (employeeSalary == null)
                return NotFound("Salary record not found.");

            return PartialView("_ViewSalaryDetailsPartial", employeeSalary);
        }

        // ✅ GET: Update Salary
        [HttpGet]
        public async Task<IActionResult> UpdateSalary(int employeeId, string? monthYear = null)
        {
            DateTime monthDate = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out var parsedMonth))
                monthDate = parsedMonth;

            var model = await _salaryRepository.GetMonthlySalaryByEmployeeIdAsync(employeeId, monthDate);
            if (model == null)
                return NotFound("Salary record not found.");

            return View(model);
        }

        // ✅ POST: Update Salary (Net Salary auto-calculated)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSalary(MonthlySalaryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Save will update NetSalary in DB
                await _salaryRepository.SaveMonthlySalaryAsync(model);
                TempData["SuccessMessage"] = "Salary updated successfully.";
                return RedirectToAction(nameof(MonthlySalaryReport));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Unable to update salary. " + ex.Message);
                return View(model);
            }
        }

        // ✅ GET: Employee Attendance + Salary Info
        [HttpGet]
        public async Task<IActionResult> EmployeeAttendanceDetails(int employeeId, string? monthYear = null)
        {
            DateTime monthDate = DateTime.Now;
            if (!string.IsNullOrEmpty(monthYear) && DateTime.TryParse(monthYear, out var parsedMonth))
                monthDate = parsedMonth;

            // Get the single employee's salary/attendance summary
            var employeeSalary = await _salaryRepository.GetMonthlySalaryByEmployeeIdAsync(employeeId, monthDate);

            if (employeeSalary == null)
                return NotFound("Attendance record not found.");

            return View(employeeSalary);
        }

        // ✅ GET: Salary History of an Employee
        [HttpGet]
        public async Task<IActionResult> SalaryHistory(int employeeId)
        {
            var history = await _salaryRepository.GetSalaryHistoryByEmployeeIdAsync(employeeId);
            return View(history);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "1")] // Restrict access to only users with UserType 1 (Admin)
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;

        public AdminController(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        // 1. Dashboard Action
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = await _adminRepository.GetAdminDashboardDataAsync();
            return View(dashboardData);
        }

        // 2. Employee Management Actions
        [HttpGet]
        public async Task<IActionResult> Employee()
        {
            var employees = await _adminRepository.GetAllEmployeesAsync();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> AddEmployee()
        {
            ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployee(Employee employee)
        {
            var existingEmployee = await _adminRepository.GetEmployeeByEmployeeIdAsync(employee.EmployeeId);
            if (existingEmployee != null)
            {
                ModelState.AddModelError("EmployeeId", "An employee with this ID already exists.");
                ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
                return View(employee);
            }

            await _adminRepository.AddEmployeeAsync(employee);
            return RedirectToAction(nameof(Employee));
        }

        // 3. Department Management Actions
        [HttpGet]
        public async Task<IActionResult> Department()
        {
            var departments = await _adminRepository.GetAllDepartmentsAsync();
            return View(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDepartment(Department department)
        {
            if (ModelState.IsValid)
            {
                await _adminRepository.AddDepartmentAsync(department);
                return RedirectToAction(nameof(Department));
            }
            return View(department);
        }

        // 4. Leave Management Actions
        [HttpGet]
        public async Task<IActionResult> Leave()
        {
            var leaves = await _adminRepository.GetAllLeaveApplicationsAsync();
            return View(leaves);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeaveStatus(int leaveId, LeaveStatus status)
        {
            await _adminRepository.UpdateLeaveStatusAsync(leaveId, status);
            return RedirectToAction(nameof(Leave));
        }

        // 5. Salary Management Action
        public IActionResult Salary()
        {
            return View();
        }

        // 6. Attendance Report Actions
        [HttpGet]
        public async Task<IActionResult> AttendanceReport()
        {
            var allAttendance = await _adminRepository.GetAllAttendanceAsync();
            return View(allAttendance);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeAttendanceDetails(int employeeId)
        {
            var attendanceDetails = await _adminRepository.GetEmployeeAttendanceAsync(employeeId);
            return View(attendanceDetails);
        }
    }
}
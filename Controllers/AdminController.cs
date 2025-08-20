using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Repositories;
using Smart_Attendance_System.Services;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "1")] // Restrict access to only users with UserType 1 (Admin)
    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IAccountRepository _accountRepository;

        public AdminController(IAdminRepository adminRepository, IAccountRepository accountRepository)
        {
            _adminRepository = adminRepository;
            _accountRepository = accountRepository;
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

        // 3. Department Management Actions - Now handled by DepartmentController
        [HttpGet]
        public IActionResult Department()
        {
            return RedirectToAction("Index", "Department");
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
     

        // New action for Employee Appointments

        [HttpGet]
        public async Task<IActionResult> EmployeeAppointment()
        {
            var pendingEmployees = await _adminRepository.GetPendingEmployeesAsync();

            var viewModel = new List<EmployeeAppointmentVM>();

            foreach (var employee in pendingEmployees)
            {
                var user = await _accountRepository.GetUserByEmployeeIdAsync(employee.Id);
                if (user != null)
                {
                    viewModel.Add(new EmployeeAppointmentVM
                    {
                        EmployeeRecordId = employee.Id,
                        EmployeeName = employee.EmployeeName,
                        EmployeeId = employee.EmployeeId,
                        Email = user.Email,
                        DepartmentName = employee.Department?.DepartmentName
                    });
                }
            }

            return View(viewModel);
        }

        // Action to handle approval or rejection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEmployee(int employeeId)
        {
            await _adminRepository.UpdateEmployeeStatusAsync(employeeId, EmployeeStatus.Approved);
            return RedirectToAction(nameof(EmployeeAppointment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEmployee(int employeeId)
        {
            await _adminRepository.UpdateEmployeeStatusAsync(employeeId, EmployeeStatus.Rejected);
            return RedirectToAction(nameof(EmployeeAppointment));
        }

        // Modified Employee action to show Approved employees
        [HttpGet]
        public async Task<IActionResult> EmployeeDetails()
        {
            var employees = await _adminRepository.GetAllEmployeesAsync(); // Assuming this retrieves all approved employees
            return View(employees.Where(e => e.Status == EmployeeStatus.Approved));
        }
    }
}
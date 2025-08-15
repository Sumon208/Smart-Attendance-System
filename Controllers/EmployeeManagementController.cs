using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "1")] // Only Admin can access this controller
    public class EmployeeManagementController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmployeeManagementController(IEmployeeRepository employeeRepository, IAdminRepository adminRepository, IWebHostEnvironment hostEnvironment)
        {
            _employeeRepository = employeeRepository;
            _adminRepository = adminRepository;
            _hostEnvironment = hostEnvironment;
        }

        // Employee Details Action
        [HttpGet]
        public async Task<IActionResult> Details()
        {
            var approvedEmployees = await _employeeRepository.GetApprovedEmployeesAsync();
            return View(approvedEmployees);
        }

        // Employee Update Actions
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _employeeRepository.UpdateEmployeeAsync(employee);
                return RedirectToAction(nameof(Details));
            }
            return View(employee);
        }

        // Employee Delete Action
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _employeeRepository.DeleteEmployeeAsync(id);
            return RedirectToAction(nameof(Details));
        }

        // New Action for Add Employee functionality
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Employee employee)
        {
            if (ModelState.IsValid)
            {
                var existingEmployee = await _employeeRepository.GetEmployeeByEmployeeIdAsync(employee.EmployeeId);
                if (existingEmployee != null)
                {
                    ModelState.AddModelError("EmployeeId", "An employee with this ID already exists.");
                    ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
                    return View(employee);
                }

                await _employeeRepository.AddEmployeeAsync(employee);
                return RedirectToAction(nameof(Details));
            }
            ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
            return View(employee);
        }
    }
}
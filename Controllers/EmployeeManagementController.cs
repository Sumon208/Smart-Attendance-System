using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admin can access this controller
    public class EmployeeManagementController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmployeeManagementController(IEmployeeRepository employeeRepository, IAdminRepository adminRepository, IWebHostEnvironment hostEnvironment,IAttendanceRepository attendanceRepository)
        {
            _employeeRepository = employeeRepository;
            _adminRepository = adminRepository;
            _hostEnvironment = hostEnvironment;
            _attendanceRepository = attendanceRepository;
        }

        // Employee Details Action
        public async Task<IActionResult> Index()
        {
            var employees = await _employeeRepository.GetAllEmployeesAsync();
            return View(employees);
        }

        //Employee Details(View + Update)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _attendanceRepository.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await _employeeRepository.UpdateEmployeeAsync(employee);
                return RedirectToAction(nameof(Details), new { id = employee.Id });
            }
            return View(employee);
        }

        // Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _employeeRepository.DeleteEmployeeAsync(id);
           TempData["SuccessMessage"] = "Employee deleted successfully!";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
           
            var employee = await _employeeRepository.GetEmployeeByIdAsync(id); // use Id (int)
            if (employee == null)
            {
                return NotFound();
            }
            var departments = await _adminRepository.GetAllDepartmentsAsync(); // or _context.Departments
            ViewBag.Department = departments.Select(d => new SelectListItem
            {
                Value = d.DepartmentId.ToString(),
                Text = d.DepartmentName
            }).ToList();
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, Employee employee)
        {
            if (id != employee.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(employee);

            var result = await _employeeRepository.UpdateEmployeeAsyn(employee);

            if (!result)
            {
                ModelState.AddModelError("", "Update failed. Check duplicate ID or file issues.");
                return View(employee);
            }

            TempData["Success"] = true;

            // redirect back to Edit page so modal can show
            return RedirectToAction(nameof(Update), new { id = employee.Id });
        }


    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.Repositories;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Employee")]
    public class TaskController : Controller
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ApplicationDbContext _context;

        public TaskController(ITaskRepository taskRepository, IAdminRepository adminRepository, IEmployeeRepository employeeRepo,ApplicationDbContext context)
        {
            _taskRepository = taskRepository;
            _adminRepository = adminRepository;
            _employeeRepo = employeeRepo;
            _context = context;
        }

        // GET: Task
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tasks = await _taskRepository.GetAllTasksAsync();
            return View(tasks ?? new List<EmployeeTask>());
        }

        [HttpGet]
        public IActionResult Create()
        {

            var empName = User.Identity?.Name ?? "Unknown";


            var empIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(empIdClaim))
            {
                return BadRequest("Employee ID not found in claims.");
            }

            // ViewBag এ পাঠানো
            ViewBag.LoggedInEmployeeId = empIdClaim;
            ViewBag.LoggedInEmployeeName = empName;
            ViewBag.Shifts = new List<string> { "Morning", "Evening", "Night" };

            // EmployeeTask model এ numeric Id লাগলে
            int empIdNumeric = 0;
            if (!int.TryParse(empIdClaim, out empIdNumeric))
            {
                empIdNumeric = 0;
            }

            var model = new EmployeeTask
            {
                EmployeeId = empIdNumeric
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeTask task)
        {

            if (task.EmployeeId == 0)
            {
                ModelState.AddModelError("", "EmployeeId is required.");
            }

            if (ModelState.IsValid)
            {
                await _taskRepository.AddTaskAsync(task);
                return RedirectToAction(nameof(Index));
            }

            // Post-back: shift & login info set
            ViewBag.Shifts = new List<string> { "Morning", "Evening", "Night" };
            ViewBag.LoggedInEmployeeName = User.Identity?.Name;
            ViewBag.LoggedInEmployeeId = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

            return View(task);
        }




        // Helper method to populate dropdowns
        private async Task PopulateDropdownsAsync()
        {
            // Employee dropdown (string EmployeeId)
            var employees = await _employeeRepo.GetAllEmployeesAsync();
            ViewBag.Employees = employees
                .Select(e => new { EmployeeId = e.EmployeeId, Name = e.EmployeeName })
                .ToList();

            // Shift dropdown
            ViewBag.Shifts = new List<string> { "Morning", "Evening", "Night" };
        }


        // GET: Task/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _taskRepository.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            await PopulateDropdownsAsync();
            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeTask task)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(task);
            }

            await _taskRepository.UpdateTaskAsync(task);
            return RedirectToAction(nameof(Index));
        }

        // POST: Task/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _taskRepository.DeleteTaskAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Helper method to populate dropdowns safely
     
    }
}

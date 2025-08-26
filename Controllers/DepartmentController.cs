using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Models;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "1")] // Restrict access to only users with UserType 1 (Admin)
    public class DepartmentController : Controller
    {
        private readonly IDepartmentRepository _department;

        public DepartmentController(IDepartmentRepository departmentService)
        {
            _department = departmentService;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
          //  var departments = await _departmentService.GetAllDepartmentsAsync();
           var dept=await _department.GetDepartmentsAsync();
            return View(dept);
        }

        // GET: Department/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var department = await _department.GetDepartmentByIdASync(id);
            if (department == null)
            {
                TempData["ErrorMessage"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Department/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string DepartmentName)
        {
            try
            {
                Console.WriteLine($"Received DepartmentName: {DepartmentName}");
                
                if (string.IsNullOrWhiteSpace(DepartmentName))
                {
                    TempData["ErrorMessage"] = "Department name cannot be empty.";
                    return RedirectToAction(nameof(Index));
                }

                var department = new Department { DepartmentName = DepartmentName };
                Console.WriteLine($"Created department object: {department.DepartmentName}");
                
                var result = await _department.AddDepartmentAsync(department);
                
                if (result)
                {
                    TempData["SuccessMessage"] = $"Department '{DepartmentName}' has been created successfully!";
                    System.Diagnostics.Debug.WriteLine("Department added successfully");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Department '{DepartmentName}' already exists!";
                    System.Diagnostics.Debug.WriteLine("Department already exists");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _department.GetDepartmentByIdASync(id);
            if (department == null)
            {
                TempData["ErrorMessage"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
            {
                TempData["ErrorMessage"] = "Invalid department ID.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(department);
            }

            var result = await _department.UpdateDepartmentAsync(department);

            if (result)
            {
                TempData["SuccessMessage"] = $"Department '{department.DepartmentName}' has been updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update department. Name might already exist.";
            return View(department);
        }


        // GET: Department/Delete/5
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var department = await _department.DeleteDepartmentASync(id);
        //    if (department == null)
        //    {
        //        TempData["ErrorMessage"] = "Department not found.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(department);
        //}

        // POST: Department/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _department.DeleteDepartmentASync(id);
            if (result)
                TempData["SuccessMessage"] = "Department has been deleted successfully!";
            else
                TempData["ErrorMessage"] = "Cannot delete department. It may have employees or doesn't exist.";

            return RedirectToAction(nameof(Index));
        }



        [HttpPost]
        public async Task<IActionResult> CheckNameExists(string departmentName)
        {
            var exists = await _department.IsDepartmentNameExistsAsync(departmentName);
            return Json(new { exists = exists });
        }
    }
}


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
            return View(employee);
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Update(int id, Employee employee)
        //{
        //    if (id != employee.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            // পুরানো Employee DB থেকে নিয়ে আসো
        //            var existingEmployee = await _employeeRepository.GetEmployeeByIdAsync(id);
        //            if (existingEmployee == null)
        //            {
        //                return NotFound();
        //            }

        //            // ✅ Basic fields update
        //            existingEmployee.EmployeeId = employee.EmployeeId;
        //            existingEmployee.EmployeeName = employee.EmployeeName;
        //            existingEmployee.DateOfBirth = employee.DateOfBirth;
        //            existingEmployee.Gender = employee.Gender;
        //            existingEmployee.Email = employee.Email;
        //            existingEmployee.Address = employee.Address;
        //            existingEmployee.JoiningDate = employee.JoiningDate;
        //            existingEmployee.DepartmentId = employee.DepartmentId;
        //            existingEmployee.Salary = employee.Salary;
        //            existingEmployee.Nationality = employee.Nationality;
        //            existingEmployee.Description = employee.Description;
        //            existingEmployee.MobileNumber = employee.MobileNumber;
        //            existingEmployee.BloodGroup = employee.BloodGroup;


        //            // ✅ Photo upload
        //            if (employee.EmployeePhotoFile != null && employee.EmployeePhotoFile.Length > 0)
        //            {
        //                var photoFileName = Guid.NewGuid().ToString() + Path.GetExtension(employee.EmployeePhotoFile.FileName);
        //                var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/photos", photoFileName);

        //                using (var stream = new FileStream(photoPath, FileMode.Create))
        //                {
        //                    await employee.EmployeePhotoFile.CopyToAsync(stream);
        //                }

        //                existingEmployee.EmployeePhotoPath = "/uploads/photos/" + photoFileName;
        //            }

        //            // ✅ Certificate upload
        //            // Certificate upload (required PDF or image)
        //            if (employee.CertificateFile != null && employee.CertificateFile.Length > 0)
        //            {
        //                // Allowed extensions
        //                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".gif" };
        //                var ext = Path.GetExtension(employee.CertificateFile.FileName).ToLower();

        //                if (!allowedExtensions.Contains(ext))
        //                {
        //                    ModelState.AddModelError("CertificateFile", "Only PDF or image files are allowed.");
        //                    return View(employee);
        //                }

        //                var certFileName = Guid.NewGuid().ToString() + ext;
        //                var certPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/certificates", certFileName);

        //                using (var stream = new FileStream(certPath, FileMode.Create))
        //                {
        //                    await employee.CertificateFile.CopyToAsync(stream);
        //                }

        //                existingEmployee.CertificateFilePath = "/uploads/certificates/" + certFileName;
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("CertificateFile", "Certificate file is required.");
        //                return View(employee);
        //            }


        //            // ✅ Save updated employee
        //            await _employeeRepository.UpdateEmployeeAsync(existingEmployee);
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", $"Update failed: {ex.Message}");
        //            return View(employee);
        //        }

        //        return RedirectToAction(nameof(Index));
        //    }

        //    return View(employee);
        //}
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

            return RedirectToAction(nameof(Index));
        }


    }
}
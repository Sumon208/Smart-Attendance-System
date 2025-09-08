using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BCrypt.Net;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.MessageService;

namespace Smart_Attendance_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountRepository _accountRepository;
        private readonly INotificationRepository _notificationRepository;

        public AccountController(IAccountRepository accountRepository,INotificationRepository notificationRepository)
        {
            _accountRepository = accountRepository;
            _notificationRepository = notificationRepository;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {

        //        // Use the new method to get the user and employee in one call
        //        var user = await _accountRepository.GetUserByEmailWithEmployeeAsync(model.Email);

        //        if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        //        {
        //            // First, check the status of regular employees
        //            if (user.UserType == 2 && user.Employee?.Status != EmployeeStatus.Approved)
        //            {
        //                ModelState.AddModelError(string.Empty, "You are not a Current Employee of this Institution.");
        //                return View(model);

        //            }

        //            // If the user is an Admin or an Approved User, proceed with login
        //            var claims = new List<Claim>
        //              {
        //                    new Claim(ClaimTypes.Name, user.Employee?.EmployeeName ?? ""), // Employee Name
        //                        new Claim(ClaimTypes.NameIdentifier, user.EmployeeId?.ToString() ?? "0"),
        //                        new Claim(ClaimTypes.Email, user.Email),
        //                        new Claim("EmployeePhotoPath", user.Employee?.EmployeePhotoPath ?? "/images/default-user.png"),
        //                        new Claim(ClaimTypes.Role, user.UserType.ToString())
        //                                      };

        //            var claimsIdentity = new ClaimsIdentity(claims, "Login");
        //            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

        //            if (user.UserType == 1) // Admin
        //            {
        //                return RedirectToAction("Dashboard", "Admin");
        //            }
        //            else if (user.UserType == 2) //User
        //            {
        //                return RedirectToAction("Dashboard", "Employee");
        //            }
        //            else
        //            {
        //                return RedirectToAction("Index", "Home");
        //            }
        //        }

        //        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //    }
        //    return View(model);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Load SystemUser + Employee together
                var user = await _accountRepository.GetUserByEmailWithEmployeeAsync(model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // ✅ If employee, check approval status
                    if (user.UserType == 2)
                    {
                        if (user.Employee?.Status == EmployeeStatus.Pending)
                        {
                            ModelState.AddModelError(string.Empty, "Your account is pending approval by the Admin.");
                            return View(model);
                        }
                        if (user.Employee?.Status == EmployeeStatus.Rejected)
                        {
                            ModelState.AddModelError(string.Empty, "Your account has been rejected. Please contact Admin.");
                            return View(model);
                        }
                    }

                    // ✅ Claims for authentication
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Employee?.EmployeeName ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.EmployeeId?.ToString() ?? "0"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("EmployeePhotoPath", user.Employee?.EmployeePhotoPath ?? "/images/default-user.png"),
                // clearer roles: Admin / Employee
                new Claim(ClaimTypes.Role, user.UserType == 1 ? "Admin" : "Employee")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, "Login");
                    await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

                    // ✅ Redirect based on role
                    if (user.UserType == 1) // Admin
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else if (user.UserType == 2) // Employee
                    {
                        return RedirectToAction("Dashboard", "Employee");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var departments = await _accountRepository.GetAllDepartmentsAsync();
            var employees = await _accountRepository.GetAllEmployeesAsync(); // Fetch the employees

            ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
            ViewBag.Employees = employees; // Pass the list of employees to the view

            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Register(RegistrationViewModel model)
        //{
        //    var departments = await _accountRepository.GetAllDepartmentsAsync();
        //    var employees = await _accountRepository.GetAllEmployeesAsync();

        //    ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
        //    ViewBag.Employees = employees;

        //    if (ModelState.IsValid)
        //    {
        //        if (!string.IsNullOrEmpty(model.EmployeeId))
        //        {
        //            var existingEmployee = await _accountRepository.GetEmployeeByEmployeeIdAsync(model.EmployeeId);
        //            if (existingEmployee != null)
        //            {
        //                ModelState.AddModelError("EmployeeId", "An account with this Employee ID already exists.");
        //                return View(model);
        //            }
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("EmployeeId", "Employee ID is required.");
        //            return View(model);
        //        }

        //        var existingUser = await _accountRepository.GetUserByEmailAsync(model.Email);
        //        if (existingUser != null)
        //        {
        //            ModelState.AddModelError("Email", "An account with this email already exists.");
        //            return View(model);
        //        }

        //        var newEmployee = new Employee
        //        {
        //            EmployeeName = model.EmployeeName,
        //            EmployeeId = model.EmployeeId,
        //            DepartmentId = model.DepartmentId,
        //            EmployeePhotoPath = "/images/default.jpg",
        //            Gender = "N/A",
        //            Nationality = "N/A",
        //            DateOfBirth = DateTime.Now,
        //            Salary = 0,
        //            Description = "New User",
        //            Status = EmployeeStatus.Pending // 👈 still pending until admin approves
        //        };

        //        var newSystemUser = new SystemUser
        //        {
        //            Email = model.Email,
        //            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
        //            UserType = 2,
        //        };

        //        var result = await _accountRepository.RegisterUserAsync(newEmployee, newSystemUser);

        //        if (result)
        //        {
        //            // ✅ Add notification for Admin
        //            await _notificationRepository.AddNotificationAsync(new Notification
        //            {
        //                ForRole = "Admin",
        //                Title = "New User Registration",
        //                Message = $"User {newEmployee.EmployeeName} has registered and is awaiting approval.",
        //                LinkUrl = Url.Action("EmployeeAppointment", "Admin", null, Request.Scheme), // you’ll need to create PendingUsers action
        //                CreatedAt = DateTime.UtcNow,
        //                IsRead = false
        //            });

        //            TempData["SuccessMessage"] = "Registration successful! Please wait for Admin approval before login.";
        //            return RedirectToAction("Login");
        //        }

        //        ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
        //    }

        //    return View(model);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            var departments = await _accountRepository.GetAllDepartmentsAsync();
            var employees = await _accountRepository.GetAllEmployeesAsync();

            ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
            ViewBag.Employees = employees;

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.EmployeeId))
                {
                    var existingEmployee = await _accountRepository.GetEmployeeByEmployeeIdAsync(model.EmployeeId);
                    if (existingEmployee != null)
                    {
                        ModelState.AddModelError("EmployeeId", "An account with this Employee ID already exists.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("EmployeeId", "Employee ID is required.");
                    return View(model);
                }

                var existingUser = await _accountRepository.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                // ✅ Call helper methods for file upload
                string photoPath = await SaveEmployeePhotoAsync(model.EmployeePhotoFile);
                string certificatePath = await SaveCertificateAsync(model.CertificateFile);

                var newEmployee = new Employee
                {
                    EmployeeName = model.EmployeeName,
                    EmployeeId = model.EmployeeId,
                    DepartmentId = model.DepartmentId,
                    Email = model.Email,
                    EmployeePhotoPath = photoPath,
                    CertificateFilePath = certificatePath,
                    Gender = "N/A",
                    Nationality = "N/A",
                    DateOfBirth = DateTime.Now,
                    Salary = 0,
                    Description = "New User",
                    Status = EmployeeStatus.Pending
                };

                var newSystemUser = new SystemUser
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    UserType = 2,
                };

                var result = await _accountRepository.RegisterUserAsync(newEmployee, newSystemUser);

                if (result)
                {
                    await _notificationRepository.AddNotificationAsync(new Notification
                    {
                        ForRole = "Admin",
                        Title = "New User Registration",
                        Message = $"User {newEmployee.EmployeeName} has registered and is awaiting approval.",
                        LinkUrl = Url.Action("EmployeeAppointment", "Admin", null, Request.Scheme),
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });

                    TempData["SuccessMessage"] = "Registration successful! Please wait for Admin approval before login.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
            }

            return View(model);
        }

        #region Private Methods

        private async Task<string> SaveEmployeePhotoAsync(IFormFile? photoFile)
        {
            if (photoFile != null && photoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/photos");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }

                return "/uploads/photos/" + uniqueFileName;
            }

            // default placeholder
            return "/images/default.jpg";
        }

        private async Task<string?> SaveCertificateAsync(IFormFile? certificateFile)
        {
            if (certificateFile != null && certificateFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/certificates");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(certificateFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await certificateFile.CopyToAsync(stream);
                }

                return "/uploads/certificates/" + uniqueFileName;
            }

            return null;
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
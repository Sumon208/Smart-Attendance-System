using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BCrypt.Net;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.MessageService;
using Smart_Attendance_System.EmailSettings;

namespace Smart_Attendance_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountRepository _accountRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;

        public AccountController(
            IAccountRepository accountRepository,
            INotificationRepository notificationRepository,
            IEmailService emailService)
        {
            _accountRepository = accountRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

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
                    await HttpContext.SignInAsync(
                   new ClaimsPrincipal(claimsIdentity),
                   new AuthenticationProperties
                   {
                       IsPersistent = model.RememberMe,   // 👈 Here
                       ExpiresUtc = model.RememberMe
                           ? DateTimeOffset.UtcNow.AddDays(7)   // keep for 7 days if checked
                           : DateTimeOffset.UtcNow.AddHours(1)  // otherwise expire after 1 hour
                   });


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
                    await _accountRepository.InitializeEmployeeSalaryAsync(newEmployee);
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
        // for Reset/Forget password functionalities

        [HttpGet]
        public  IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _accountRepository.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "No account found with this email.";
                return View(model);
            }

            var token = Guid.NewGuid().ToString();
            await _accountRepository.SavePasswordResetTokenAsync(user.Id, token, DateTime.UtcNow.AddHours(1));

            var resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

            var subject = "Password Reset Request";
            var body = $@"
                        <p>Hello,</p>
                        <p>You requested to reset your password. Click the link below to reset:</p>
                        <p><a href='{resetLink}'>Reset Password</a></p>
                        <p>If you didn’t request this, you can safely ignore this email.</p>
                        <p>Best Regards,<br/>Smart Attendance System</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var resetToken = await _accountRepository.GetValidResetTokenAsync(model.Token);
            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset token.";
                return RedirectToAction("Login");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _accountRepository.UpdateUserPasswordAsync(resetToken.UserId, hashedPassword);
            await _accountRepository.InvalidateResetTokenAsync(model.Token);

            TempData["SuccessMessage"] = "Password has been reset successfully. Please login.";
            return RedirectToAction("Login");
        }

    }
}
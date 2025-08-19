using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BCrypt.Net;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;

namespace Smart_Attendance_System.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountRepository _accountRepository;

        public AccountController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
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
        //        var user = await _accountRepository.GetUserByEmailAsync(model.Email);

        //        if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        //        {

        //            var claims = new List<Claim>
        //            {
        //                new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString()),
        //                new Claim(ClaimTypes.Email, user.Email),
        //                new Claim(ClaimTypes.Role, user.UserType.ToString())
        //            };

        //            var claimsIdentity = new ClaimsIdentity(claims, "Login");
        //            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

        //            if (user.UserType == 1) // Admin
        //            {
        //                return RedirectToAction("Dashboard", "Admin");
        //            }
        //            else // User
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

                // Use the new method to get the user and employee in one call
                var user = await _accountRepository.GetUserByEmailWithEmployeeAsync(model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // First, check the status of regular employees
                    if (user.UserType == 2 && user.Employee?.Status != EmployeeStatus.Approved)
                    {
                        ModelState.AddModelError(string.Empty, "You are not a Current Employee of this Institution.");
                        return View(model);
                       
                    }

                    // If the user is an Admin or an Approved User, proceed with login
                    var claims = new List<Claim>
                      {
                        new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.UserType.ToString())
                      };

                    var claimsIdentity = new ClaimsIdentity(claims, "Login");
                    await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

                    if (user.UserType == 1) // Admin
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else // User
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
            // Fetch departments and employees once to use for all view returns
            var departments = await _accountRepository.GetAllDepartmentsAsync();
            var employees = await _accountRepository.GetAllEmployeesAsync();

            // The ViewBag must be populated before any 'return View(model)'
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

                var newEmployee = new Employee
                {
                    EmployeeName = model.EmployeeName,
                    EmployeeId = model.EmployeeId,
                    DepartmentId = model.DepartmentId,
                    EmployeePhotoPath = "/images/default.jpg",
                    Gender = "N/A",
                    Nationality = "N/A",
                    DateOfBirth = DateTime.Now,
                    Salary = 0,
                    Description = "New User",
                    Status = EmployeeStatus.Pending // The corrected line is here.
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
                    TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
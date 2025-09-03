using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smart_Attendance_System.Data;
using Smart_Attendance_System.Data.SMTP_Service;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using Smart_Attendance_System.Services.MessageService;
using Smart_Attendance_System.Services.Repositores;
using Smart_Attendance_System.Services.Repositories;


namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "1")] 

    public class AdminController : Controller
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly EmailService _emailService;


        public AdminController(IAdminRepository adminRepository, IAccountRepository accountRepository, INotificationRepository notificationRepository)
        {
            _adminRepository = adminRepository;
            _accountRepository = accountRepository;
            _notificationRepository = notificationRepository;
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

        [HttpGet]
        public async Task<IActionResult> EditEmployee(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
                return BadRequest("Employee ID is required.");

            var employee = await _adminRepository.GetEmployeeByEmployeeIdAsync(employeeId);
            if (employee == null)
                return NotFound("Employee not found.");

            ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
            return View(employee);
        }

        // POST: /Admin/EditEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee([Bind("EmployeeId,EmployeeName,DepartmentId,DateOfBirth,Gender,Salary,Nationality,Description")] Employee model)
        {
            if (string.IsNullOrEmpty(model.EmployeeId))
                return BadRequest("Employee ID is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _adminRepository.GetAllDepartmentsAsync();
                return View(model);
            }

            var employee = await _adminRepository.GetEmployeeByEmployeeIdAsync(model.EmployeeId);
            if (employee == null)
                return NotFound("Employee not found.");

            // Update fields
            employee.EmployeeName = model.EmployeeName;
            employee.DepartmentId = model.DepartmentId;
            employee.DateOfBirth = model.DateOfBirth;
            employee.Gender = model.Gender;
            employee.Salary = model.Salary;
            employee.Nationality = model.Nationality;
            employee.Description = model.Description;

            await _adminRepository.UpdateEmployeeAsync(employee);

            return RedirectToAction("Employee");
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
                TempData["SuccessMessage"] = $"Department '{department.DepartmentName}' has been added successfully!";
                return RedirectToAction(nameof(Department));
            }
            // If validation fails, redirect back to Department view with error
            TempData["ErrorMessage"] = "Please provide a valid department name.";
            return RedirectToAction(nameof(Department));
        }


        // 4. Leave Management Actions
        [HttpGet]
        public async Task<IActionResult> Leave()
        {
            var leaves = await _adminRepository.GetAllLeaveApplicationsAsync();
            return View(leaves);
        }



        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UpdateLeaveStatus(int leaveId, LeaveStatus status)
        //{
        //    await _adminRepository.UpdateLeaveStatusAsync(leaveId, status);
        //    return RedirectToAction(nameof(Leave));
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeaveStatus(int leaveId, LeaveStatus status)
        {
            await _adminRepository.UpdateLeaveStatusAsync(leaveId, status);

            var leave = await _adminRepository.GetLeaveByIdAsync(leaveId);
            if (leave != null)
            {
                // 🔔 Notify employee in-app
                await _notificationRepository.AddNotificationAsync(new Notification
                {
                    EmployeeId = leave.EmployeeId,
                    Title = $"Leave {status}",
                    Message = $"Your leave request ({leave.LeaveType}, {leave.StartDate:dd MMM}-{leave.EndDate:dd MMM}) was {status}",
                    LinkUrl = Url.Action("LeaveHistory", "Leave", null, Request.Scheme)
                });

                // 📧 Send email to employee
                var subject = $"Leave {status}";
                var body = $@"
                                <p>Hello {leave.Employee?.EmployeeName},</p>
                                <p>Your leave request (<b>{leave.LeaveType}</b>) 
                                from <b>{leave.StartDate:dd MMM yyyy}</b> to <b>{leave.EndDate:dd MMM yyyy}</b> 
                                has been <span style='color:blue;font-weight:bold'>{status}</span>.</p>
                                <p><b>Reason:</b> {leave.Reason}</p>
                                <p>Regards,<br/>Smart Attendance System</p>
                            ";

                await _emailService.SendEmailAsync(leave.Employee.Email, subject, body);
            }

            return RedirectToAction(nameof(Leave));
        }

        // GET: /Admin/GetNotifications
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetNotifications()
        {
            var items = await _notificationRepository.GetNotificationsForAdminAsync();

            // ✅ Only take unread notifications
            var unread = items.Where(n => !n.IsRead);

            var dto = unread.Select(n => new
            {
                id = n.NotificationId,
                title = n.Title,
                message = n.Message,
                linkUrl = string.IsNullOrWhiteSpace(n.LinkUrl)
                            ? Url.Action("Leave", "Admin", null, Request.Scheme)
                            : n.LinkUrl,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            });

            return Json(dto);
        }


        // OPTIONAL: if you want a badge with only unread count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationRepository.GetUnreadCountForAdminAsync();
            return Json(new { count });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _adminRepository.MarkNotificationAsReadAsync(id);
            return Json(new { success });
        }

        // Monthly Salary Report
        [HttpGet]
        public async Task<IActionResult> MonthlySalaryReport(string? dateFrom = null, string? dateTo = null)
        {
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to;

            var salaryData = await _adminRepository.GetMonthlySalaryReportAsync(fromDate, toDate);

            // Pass extra info to ViewBag if needed
            ViewBag.DateFrom = fromDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.DateTo = toDate?.ToString("yyyy-MM-dd") ?? "";

            return View(salaryData);
        }

        [HttpGet]
        public async Task<IActionResult> ViewSalaryDetails(int id, string? dateFrom = null, string? dateTo = null)
        {
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to;

            // Get all salaries
            var salaries = await _adminRepository.GetMonthlySalaryReportAsync(fromDate, toDate);

            // Find the requested employee
            var employeeSalary = salaries.FirstOrDefault(x => x.EmployeeId == id);
            if (employeeSalary == null)
                return NotFound("Salary record not found.");

            return PartialView("_ViewSalaryDetailsPartial", employeeSalary);
        }
   // for update salary

        [HttpGet]
        public async Task<IActionResult> UpdateSalary(int id)
        {
            var model = await _adminRepository.GetMonthlySalaryByEmployeeIdAsync(id);
            if (model == null) return NotFound("Salary record not found.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSalary(MonthlySalaryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Return view with validation errors
                return View(model);
            }

            try
            {
                await _adminRepository.UpdateMonthlySalaryAsync(model);
                TempData["SuccessMessage"] = "Salary updated successfully.";
                return RedirectToAction(nameof(MonthlySalaryReport));
            }
            catch (Exception ex)
            {
                // log exception as needed
                ModelState.AddModelError("", "Unable to update salary. " + ex.Message);
                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> EmployeeAttendanceDetails(int employeeId)
        {
            var attendanceDetails = await _adminRepository.GetEmployeeAttendanceAsync(employeeId);
            return View(attendanceDetails);
        }



            // Monthly attendance report for admin
        [HttpGet]
        public async Task<IActionResult> MonthlyAttendanceReport(string? employeeSearch = null, string? dateFrom = null, string? dateTo = null)
        {
            DateTime? fromDate = null;
            DateTime? toDate = null;

            // Parse date parameters
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
            {
                fromDate = from;
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
            {
                toDate = to;
            }

            // Get monthly attendance report using the attendance repository
            var attendanceRepository = HttpContext.RequestServices.GetRequiredService<IAttendanceRepository>();
            var attendanceData = await attendanceRepository.GetMonthlyAttendanceReportAsync(employeeSearch, fromDate, toDate);

            // Create view model for the report with actual monthly data
            var viewModel = new MonthlyAttendanceReportViewModel
            {
                Attendances = attendanceData.ToList(),
                EmployeeSearch = employeeSearch ?? "",
                DateFrom = dateFrom ?? "",
                DateTo = dateTo ?? "",
                TotalRecords = attendanceData.Count(),
                PresentCount = attendanceData.Count(a => a.Status == "Present"),
                LateCount = attendanceData.Count(a => a.Status == "Late"),
                AbsentCount = attendanceData.Count(a => a.Status == "Absent")
            };

            return View(viewModel);
        }
        public async Task<IActionResult> GetEmployeeMonthlyAttendance(int employeeId, string? dateFrom, string? dateTo)
        {
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to;

            var attendanceRepo = HttpContext.RequestServices.GetRequiredService<IAttendanceRepository>();
            var attendances = await attendanceRepo.GetMonthlyAttendanceReportAsync(null, fromDate, toDate);

            // Filter only this employee
            var employeeAttendances = attendances.Where(a => a.EmployeeId == employeeId).ToList();

            return PartialView("_EmployeeAttendancePartial", employeeAttendances);
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
                        DepartmentName = employee.Department?.DepartmentName ?? "N/A"
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

        [HttpGet]
        public async Task<IActionResult> AttendanceReport()
        {
            var today = DateTime.Today;


            var employees = await _adminRepository.GetAllEmployeesAsync();


            var attendance = await _adminRepository.GetAttendanceByDateAsync(today);


            var report = employees.Select(emp =>
            {
                var record = attendance.FirstOrDefault(a => a.EmployeeId == emp.Id);

                if (record == null)
                {

                    return new Attendance
                    {
                        Employee = emp,
                        AttendanceDate = today,
                        Status = "Absent"
                    };
                }
                else
                {

                    string status;
                    if (record.CheckInTime.HasValue)
                    {
                        if (record.CheckInTime.Value.TimeOfDay > new TimeSpan(9, 30, 0))
                            status = "Late";
                        else
                            status = "Present";
                    }
                    else
                    {
                        status = "Absent";
                    }

                    record.Status = status;
                    return record;
                }
            }).ToList();

            return View(report);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetEmployeeMonthlyAttendance(int employeeId, string? dateFrom, string? dateTo)
        //{
        //    DateTime? fromDate = null;
        //    DateTime? toDate = null;

        //    if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
        //        fromDate = from;

        //    if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
        //        toDate = to;

        //    var attendanceRepo = HttpContext.RequestServices.GetRequiredService<IAttendanceRepository>();
        //    var attendances = await attendanceRepo.GetMonthlyAttendanceReportAsync(null, fromDate, toDate);

        //    // Filter only this employee
        //    var employeeAttendances = attendances.Where(a => a.EmployeeId == employeeId).ToList();

        //    return PartialView("_EmployeeAttendancePartial", employeeAttendances);
        //}

        [HttpGet]
        public async Task<IActionResult> EmployeeBasicInfo()
        {
            var employees = await _adminRepository.GetAllEmployeeBasicInfoAsync();
            return View(employees);
        }



    }
}
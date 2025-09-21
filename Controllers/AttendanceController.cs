using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Smart_Attendance_System.Models;
using Smart_Attendance_System.Models.ViewModel;
using Smart_Attendance_System.Services.Interfaces;
using System.Security.Claims;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Smart_Attendance_System.Services.Repositores;

namespace Smart_Attendance_System.Controllers
{
    [Authorize(Roles = "Employee")] // Restrict access to only Employee users (UserType 2)
    public class AttendanceController : Controller
    {

        private readonly IAttendanceRepository _attendancerepository;
        private readonly ISalaryRepository _salaryRepository;

        public AttendanceController(IAttendanceRepository attendanceRepository,ISalaryRepository salaryRepository)
        {
            _attendancerepository = attendanceRepository;
            _salaryRepository = salaryRepository;
        }

        public async Task<IActionResult> Attendance()
        {
            // Get current employee ID from claims
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get today's attendance
            var todayAttendance = await _attendancerepository.GetTodayAttendanceAsync(employeeIdInt);
            
            // Get recent attendance history
            var recentAttendance = await _attendancerepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 7);
            
            var vm = new UserAttendanceViewModel
            {
                EmployeeId = employeeIdInt,
                IsCheckedIn = todayAttendance?.CheckInTime.HasValue ?? false,
                IsCheckedOut = todayAttendance?.CheckOutTime.HasValue ?? false,
                CheckInTime = todayAttendance?.CheckInTime,
                CheckOutTime = todayAttendance?.CheckOutTime,
                WorkingHours = CalculateWorkingHours(todayAttendance?.CheckInTime, todayAttendance?.CheckOutTime),
                IsLate = todayAttendance?.Status == "Late",
                Status = todayAttendance?.Status ?? "Normal",
                RecentAttendance = recentAttendance.ToList()
            };
            
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var employeeIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeIdStr))
                return RedirectToAction("Login", "Account");

            var employeeId = int.Parse(employeeIdStr);

            try
            {
                var hasLeaveToday = await _attendancerepository.HasApprovedLeaveTodayAsync(employeeId);

                if (hasLeaveToday)
                {
                    TempData["ErrorMessage"] = "You are on approved leave today. You cannot check in.";
                    return RedirectToAction("Attendance");
                }

                var todayAttendance = await _attendancerepository.GetTodayAttendanceAsync(employeeId);

                if (todayAttendance != null && todayAttendance.CheckInTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You have already checked in today.";
                    return RedirectToAction("Attendance");
                }

                var now = DateTime.Now;
                var isLate = now.TimeOfDay > new TimeSpan(9, 30, 0);

                if (todayAttendance == null)
                {
                    todayAttendance = new Attendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = DateTime.Today,
                        CheckInTime = now,
                        Status = isLate ? "Late" : "Present"
                    };
                    await _attendancerepository.CreateAttendanceAsync(todayAttendance);
                }
                else
                {
                    todayAttendance.CheckInTime = now;
                    todayAttendance.Status = isLate ? "Late" : "Present";
                    await _attendancerepository.UpdateAttendanceAsync(todayAttendance);
                }

                // 🔑 Update salary for this month including approved leaves
                DateTime salaryMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                await _salaryRepository.UpdateEmployeeMonthlySalaryAsync(employeeId, salaryMonth);

                TempData["SuccessMessage"] = isLate ?
                    "Check-in successful! You arrived late today." :
                    "Check-in successful! Welcome to work.";

                return RedirectToAction("Attendance");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Check-in failed. Please try again.";
                return RedirectToAction("Attendance");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var employeeIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeIdStr))
                return RedirectToAction("Login", "Account");

            var employeeId = int.Parse(employeeIdStr);

            try
            {
                // 🔑 Step 1: Check approved leave
                var hasLeaveToday = await _attendancerepository.HasApprovedLeaveTodayAsync(employeeId);
                if (hasLeaveToday)
                {
                    TempData["ErrorMessage"] = "You are on approved leave today. You cannot check out.";
                    return RedirectToAction("Attendance");
                }

                // 🔑 Step 2: Get today's attendance
                var todayAttendance = await _attendancerepository.GetTodayAttendanceAsync(employeeId);

                if (todayAttendance == null || !todayAttendance.CheckInTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You must check in before checking out.";
                    return RedirectToAction("Attendance");
                }

                if (todayAttendance.CheckOutTime.HasValue)
                {
                    TempData["ErrorMessage"] = "You have already checked out today.";
                    return RedirectToAction("Attendance");
                }

                // 🔑 Step 3: Perform checkout
                var now = DateTime.Now;
                todayAttendance.CheckOutTime = now;

                // Calculate working hours
                todayAttendance.WorkingHours = (now - todayAttendance.CheckInTime.Value).TotalHours;

                await _attendancerepository.UpdateAttendanceAsync(todayAttendance);

                TempData["SuccessMessage"] =
                    $"Check-out successful! You worked for {todayAttendance.WorkingHours:F1} hours today. Have a great day!";

                return RedirectToAction("Attendance");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Check-out failed. Please try again.";
                return RedirectToAction("Attendance");
            }
        }

        public async Task<IActionResult> AttendanceHistory(int page = 1, int pageSize = 5, string status = "", string dateFrom = "", string dateTo = "")
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get all attendance history for filtering
            var allAttendance = await _attendancerepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 365);
            
            // Apply filters
            var filteredAttendance = allAttendance.AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredAttendance = filteredAttendance.Where(a => a.Status == status);
            }
            
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate >= fromDate);
            }
            
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate <= toDate);
            }
            
            // Calculate statistics
            var totalRecords = filteredAttendance.Count();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            
            // Apply pagination
            var paginatedAttendance = filteredAttendance
                .OrderByDescending(a => a.AttendanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Calculate statistics for the filtered data
            var presentDays = filteredAttendance.Count(a => a.Status == "Present");
            var absentDays = filteredAttendance.Count(a => a.Status == "Absent");
            var lateDays = filteredAttendance.Count(a => a.Status == "Late");
            var attendanceRate = totalRecords > 0 ? Math.Round((double)presentDays / totalRecords * 100, 1) : 0;
            
            // Calculate average working hours
            var workingDays = filteredAttendance.Where(a => a.CheckInTime.HasValue && a.CheckOutTime.HasValue);
            var averageWorkingHours = workingDays.Any() ? workingDays.Average(a => (a.CheckOutTime!.Value - a.CheckInTime!.Value).TotalHours) : 0;
            
            // Create view model for pagination and filtering
            var viewModel = new AttendanceHistoryViewModel
            {
                Attendances = paginatedAttendance,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                StatusOptions = new List<string> { "All", "Present", "Late", "Absent" },
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                AttendanceRate = attendanceRate,
                AverageWorkingHours = Math.Round(averageWorkingHours, 1)
            };

            return View(viewModel);
        }

        // Export attendance data to PDF
        [HttpGet]
        public async Task<IActionResult> ExportAttendancePDF(string status = "", string dateFrom = "", string dateTo = "")
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(employeeId))
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeIdInt = int.Parse(employeeId);
            
            // Get employee information
            var employee = await _attendancerepository.GetEmployeeByIdAsync(employeeIdInt);
            
            // Get filtered attendance data
            var allAttendance = await _attendancerepository.GetEmployeeAttendanceHistoryAsync(employeeIdInt, 365);
            var filteredAttendance = allAttendance.AsQueryable();
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                filteredAttendance = filteredAttendance.Where(a => a.Status == status);
            }
            
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate >= fromDate);
            }
            
            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var toDate))
            {
                filteredAttendance = filteredAttendance.Where(a => a.AttendanceDate <= toDate);
            }
            
            var attendanceList = filteredAttendance.OrderByDescending(a => a.AttendanceDate).ToList();
            
            // Calculate statistics
            var presentDays = attendanceList.Count(a => a.Status == "Present");
            var lateDays = attendanceList.Count(a => a.Status == "Late");
            var absentDays = attendanceList.Count(a => a.Status == "Absent");
            var totalDays = attendanceList.Count();
            var attendanceRate = (presentDays + lateDays + absentDays) > 0 ? Math.Round((double)(presentDays + lateDays) / (presentDays + lateDays + absentDays) * 100, 1) : 0;
            
            // Generate PDF
            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                // Title and Header
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64));
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, new BaseColor(0, 0, 0));
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(0, 0, 0));
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128));

                // Company Header
                var headerTable = new PdfPTable(2);
                headerTable.WidthPercentage = 100;
                headerTable.SetWidths(new float[] { 70f, 30f });

                var titleCell = new PdfPCell(new Phrase("Smart Attendance System", titleFont));
                titleCell.Border = Rectangle.NO_BORDER;
                titleCell.HorizontalAlignment = Element.ALIGN_LEFT;
                headerTable.AddCell(titleCell);

                var dateCell = new PdfPCell(new Phrase($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont));
                dateCell.Border = Rectangle.NO_BORDER;
                dateCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                headerTable.AddCell(dateCell);

                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // Report Title
                var reportTitle = new Paragraph("ATTENDANCE REPORT", headerFont);
                reportTitle.Alignment = Element.ALIGN_CENTER;
                document.Add(reportTitle);
                document.Add(new Paragraph("\n"));

                // Employee Information
                var empInfoTable = new PdfPTable(2);
                empInfoTable.WidthPercentage = 100;
                empInfoTable.SetWidths(new float[] { 30f, 70f });

                empInfoTable.AddCell(new PdfPCell(new Phrase("Employee Name:", normalFont)) { Border = Rectangle.NO_BORDER });
                empInfoTable.AddCell(new PdfPCell(new Phrase($"{employee?.EmployeeName}", normalFont)) { Border = Rectangle.NO_BORDER });
                
                empInfoTable.AddCell(new PdfPCell(new Phrase("Employee ID:", normalFont)) { Border = Rectangle.NO_BORDER });
                empInfoTable.AddCell(new PdfPCell(new Phrase(employeeId, normalFont)) { Border = Rectangle.NO_BORDER });
                
                empInfoTable.AddCell(new PdfPCell(new Phrase("Report Period:", normalFont)) { Border = Rectangle.NO_BORDER });
                var periodText = !string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo) 
                    ? $"{dateFrom} to {dateTo}" 
                    : "All Available Records";
                empInfoTable.AddCell(new PdfPCell(new Phrase(periodText, normalFont)) { Border = Rectangle.NO_BORDER });

                document.Add(empInfoTable);
                document.Add(new Paragraph("\n"));

                // Statistics Summary
                var statsTitle = new Paragraph("ATTENDANCE SUMMARY", headerFont);
                statsTitle.Alignment = Element.ALIGN_LEFT;
                document.Add(statsTitle);

                var statsTable = new PdfPTable(4);
                statsTable.WidthPercentage = 100;
                statsTable.SetWidths(new float[] { 25f, 25f, 25f, 25f });

                // Header row
                var lightGray = new BaseColor(211, 211, 211);
                statsTable.AddCell(new PdfPCell(new Phrase("Present Days", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase("Late Days", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase("Absent Days", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase("Attendance Rate", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });

                // Data row
                statsTable.AddCell(new PdfPCell(new Phrase($"{presentDays + lateDays}", normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase($"{lateDays}", normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase($"{absentDays}", normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                statsTable.AddCell(new PdfPCell(new Phrase($"{attendanceRate}%", normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });

                document.Add(statsTable);
                document.Add(new Paragraph("\n"));

                // Attendance Details
                if (attendanceList.Any())
                {
                    var detailsTitle = new Paragraph("ATTENDANCE DETAILS", headerFont);
                    detailsTitle.Alignment = Element.ALIGN_LEFT;
                    document.Add(detailsTitle);

                    var table = new PdfPTable(6);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 15f, 12f, 15f, 15f, 15f, 15f });

                    // Header
                    table.AddCell(new PdfPCell(new Phrase("Date", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Day", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Check In", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Check Out", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Working Hours", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase("Status", headerFont)) { BackgroundColor = lightGray, HorizontalAlignment = Element.ALIGN_CENTER });

                    foreach (var attendance in attendanceList)
                    {
                        var checkIn = attendance.CheckInTime?.ToString("HH:mm") ?? "—";
                        var checkOut = attendance.CheckOutTime?.ToString("HH:mm") ?? "—";
                        var workingHours = attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue 
                            ? $"{(attendance.CheckOutTime!.Value - attendance.CheckInTime!.Value).TotalHours:F1}h" 
                            : "—";

                        table.AddCell(new PdfPCell(new Phrase(attendance.AttendanceDate.ToString("yyyy-MM-dd"), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(attendance.AttendanceDate.ToString("ddd"), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(checkIn, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(checkOut, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        table.AddCell(new PdfPCell(new Phrase(workingHours, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                        
                        var statusColor = attendance.Status switch
                        {
                            "Present" => new BaseColor(144, 238, 144),  // Light green
                            "Late" => new BaseColor(255, 165, 0),       // Orange
                            "Absent" => new BaseColor(255, 182, 193),   // Light red
                            _ => new BaseColor(211, 211, 211)           // Light gray
                        };
                        table.AddCell(new PdfPCell(new Phrase(attendance.Status, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = statusColor });
                    }

                    document.Add(table);
                }
                else
                {
                    document.Add(new Paragraph("No attendance records found for the specified criteria.", normalFont));
                }

                // Footer
                document.Add(new Paragraph("\n"));
                var footer = new Paragraph("This report was generated automatically by Smart Attendance System.", smallFont);
                footer.Alignment = Element.ALIGN_CENTER;
                document.Add(footer);

                document.Close();
                
                var fileName = $"attendance_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(memoryStream.ToArray(), "application/pdf", fileName);
            }
        }

        // Helper method to calculate working hours
        private double CalculateWorkingHours(DateTime? checkInTime, DateTime? checkOutTime)
        {
            if (!checkInTime.HasValue || !checkOutTime.HasValue)
                return 0;

            var duration = checkOutTime!.Value - checkInTime!.Value;
            return duration.TotalHours;
        }


    }
}

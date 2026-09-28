using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using ClosedXML.Excel;

namespace CRMPortal.Controllers
{
    public class HRController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HRController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Dashboard()
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var model = new HRDashboardViewModel();

                model.MasterEmployee = new MasterEmployee
                {
                    JoiningDate = DateOnly.FromDateTime(DateTime.Today),
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Today)
                };

                model.EmployeeAttendance = new EmployeeAttendance
                {
                    AttendanceDate = DateOnly.FromDateTime(DateTime.Today)
                };



                model.EmployeeList = _context.MasterEmployee.Where(x => !x.IsDeleted).ToList();



                // Total Employees
                //ViewBag.TotalEmployees =_context.MasterEmployee.Count();

                //// Active Employees
                //ViewBag.ActiveEmployees = _context.MasterEmployee.Count(x => x.EmployeeStatus == "Active" && x.IsDeleted == false);

                //// Inactive Employees
                //ViewBag.InactiveEmployees = _context.MasterEmployee.Count(x => x.EmployeeStatus == "Inactive" && x.IsDeleted == true);

                return View(model);
            }
            catch
            {
                return RedirectToAction("Login", "Account");
            }
        }
        [HttpGet]
        public IActionResult Employees()
        {
            try
            {

                int? roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId != 5 && roleId != 3)


                    if (HttpContext.Session.GetInt32("RoleId") != 5)

                        if (HttpContext.Session.GetInt32("RoleId") != 4)
                        {
                            return RedirectToAction("Login", "Account");
                        }

                // Total Employees
                ViewBag.TotalEmployees = _context.MasterEmployee.Count();

                // Active Employees
                ViewBag.ActiveEmployees = _context.MasterEmployee.Count(x => x.EmployeeStatus == "Active" && x.IsDeleted == false);

                // Inactive Employees
                ViewBag.InactiveEmployees = _context.MasterEmployee.Count(x => x.EmployeeStatus == "Inactive" && x.IsDeleted == true);

                var employees = _context.MasterEmployee
            .OrderBy(x => x.IsDeleted)              // Active (false) first, Inactive (true) last
            .ThenByDescending(x => x.EmployeeCode)  // Employee Code descending within each group
            .ToList();
                return View(employees);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }

        // Edit Employee Code 

        public async Task<IActionResult> EditEmployee(int id)
        {
            try
            {
                // Allow only HR (RoleId = 5)
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                HRDashboardViewModel vm = new HRDashboardViewModel();

                vm.MasterEmployee = employee;
                vm.EmployeeAttendance = new EmployeeAttendance();

                vm.EmployeeList = await _context.MasterEmployee
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.FullName)
                    .ToListAsync();

                ViewBag.TotalEmployees = await _context.MasterEmployee
                    .CountAsync(x => !x.IsDeleted);

                ViewBag.ActiveEmployees = await _context.MasterEmployee
                    .CountAsync(x =>
                        x.EmployeeStatus == "Active" &&
                        !x.IsDeleted);

                ViewBag.InactiveEmployees = await _context.MasterEmployee
                    .CountAsync(x =>
                        x.EmployeeStatus == "Inactive" &&
                        !x.IsDeleted);

                return View("Dashboard", vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Something went wrong while loading employee details.";

                // Optional: Log exception
                Console.WriteLine(ex.Message);

                return RedirectToAction("Employees");
            }
        }

        // Update employee Method 
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(HRDashboardViewModel model)
        {
            try
            {
                // Allow only HR (RoleId = 5)
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = model.MasterEmployee;

                var dbEmployee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.EmployeeId);

                if (dbEmployee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                // Duplicate Employee Code Check
                bool employeeCodeExists = await _context.MasterEmployee.AnyAsync(x =>
                    x.EmployeeCode == employee.EmployeeCode &&
                    x.EmployeeId != employee.EmployeeId &&
                    !x.IsDeleted);

                if (employeeCodeExists)
                {
                    TempData["Error"] = "Employee Code already exists.";
                    return RedirectToAction("Dashboard");
                }

                // Duplicate Employee Name Check
                bool employeeNameExists = await _context.MasterEmployee.AnyAsync(x =>
                    x.FullName == employee.FullName &&
                    x.EmployeeId != employee.EmployeeId &&
                    !x.IsDeleted);

                if (employeeNameExists)
                {
                    TempData["Error"] = "Employee Name already exists.";
                    return RedirectToAction("Dashboard");
                }

                // Update Employee Details
                dbEmployee.EmployeeCode = employee.EmployeeCode;
                dbEmployee.FullName = employee.FullName;
                dbEmployee.Email = employee.Email;
                dbEmployee.MobileNumber = employee.MobileNumber;
                dbEmployee.Gender = employee.Gender;
                dbEmployee.DateOfBirth = employee.DateOfBirth;
                dbEmployee.BloodGroup = employee.BloodGroup;
                dbEmployee.MaritalStatus = employee.MaritalStatus;
                dbEmployee.AddressLine1 = employee.AddressLine1;
                dbEmployee.Pincode = employee.Pincode;
                dbEmployee.Department = employee.Department;
                dbEmployee.Designation = employee.Designation;
                dbEmployee.JoiningDate = employee.JoiningDate;
                // Employment Type
                if (!string.IsNullOrWhiteSpace(employee.EmploymentType))
                {
                    dbEmployee.EmploymentType = employee.EmploymentType;
                }
                dbEmployee.ReportingManager = employee.ReportingManager;
                dbEmployee.Shift = employee.Shift;
                dbEmployee.Salary = employee.Salary;
                dbEmployee.AadhaarNumber = employee.AadhaarNumber;
                dbEmployee.PANNumber = employee.PANNumber;
                dbEmployee.BankName = employee.BankName;
                dbEmployee.AccountNumber = employee.AccountNumber;
                dbEmployee.IFSCCode = employee.IFSCCode;
                dbEmployee.EmployeeStatus = employee.EmployeeStatus;

                // Update Audit Fields
                dbEmployee.ModifiedDate = DateTime.Now;
                dbEmployee.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee Updated Successfully.";

                return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Something went wrong while updating the employee.";

                // Optional: Log the exception for debugging
                Console.WriteLine(ex.Message);

                return RedirectToAction("Employees");
            }
        }

        public IActionResult DeleteEmployee(int id)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = _context.MasterEmployee
                    .FirstOrDefault(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                // Check if employee is already inactive
                if (employee.IsDeleted || employee.EmployeeStatus == "InActive")
                {
                    TempData["Error"] = "Employee is already inactive.";
                    return RedirectToAction("Employees");
                }

                // Soft Delete
                employee.IsDeleted = true;

                // Change Employee Status
                employee.EmployeeStatus = "Inactive";

                employee.ModifiedDate = DateTime.Now;
                employee.UpdatedDate = DateTime.Now;

                _context.SaveChanges();

                TempData["Success"] = "Employee marked as Inactive successfully.";

                return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Employees");
            }
        }
        [HttpPost]
        public IActionResult AddEmployee(HRDashboardViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = model.MasterEmployee;

                // Check duplicate Employee Code
                bool employeeCodeExists = _context.MasterEmployee
                    .Any(x => x.EmployeeCode == employee.EmployeeCode &&
                              x.IsDeleted == false);

                if (employeeCodeExists)
                {
                    TempData["Error"] = "Employee code already exists.";

                    return RedirectToAction("Dashboard");
                }

                // Check duplicate Full Name
                bool employeeNameExists = _context.MasterEmployee
                    .Any(x => x.FullName == employee.FullName &&
                              x.IsDeleted == false);

                if (employeeNameExists)
                {
                    TempData["Error"] = "Employee name already exists.";

                    return RedirectToAction("Dashboard");
                }


                employee.CreatedDate = DateTime.Now;

                employee.UpdatedDate = DateTime.Now;

                employee.ModifiedDate = DateTime.Now;

                employee.IsDeleted = false;

                _context.MasterEmployee.Add(employee);

                _context.SaveChanges();

                TempData["Success"] = "Employee Added Successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }

        // Attendences codes 

        public IActionResult Attendance()
        {
            if (HttpContext.Session.GetInt32("RoleId") != 5)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.EmployeeList = _context.MasterEmployee
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult SaveAttendance(HRDashboardViewModel model)
        {
            try
            {
                // Only HR can mark attendance
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var attendance = model.EmployeeAttendance;

                // Check employee exists
                var employee = _context.MasterEmployee
                    .FirstOrDefault(x => x.EmployeeId == attendance.EmployeeId && !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Dashboard");
                }

                // Check attendance already marked
                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == attendance.EmployeeId &&
                    x.AttendanceDate == attendance.AttendanceDate &&
                    !x.IsDeleted);

                if (exists)
                {
                    TempData["Error"] = "Attendance already marked for this employee on the selected date.";
                    return RedirectToAction("Dashboard");
                }

                // Save attendance
                attendance.CreatedDate = DateTime.Now;
                attendance.UpdatedDate = DateTime.Now;
                attendance.ModifiedDate = DateTime.Now;
                attendance.IsDeleted = false;

                _context.EmployeeAttendance.Add(attendance);
                _context.SaveChanges();

                // =====================================================
                // PAID LEAVE (PL)
                // =====================================================
                if (attendance.Status == "PL")
                {
                    if (employee.PaidLeaveBalance >= 1)
                    {
                        employee.PaidLeaveBalance -= 1;
                    }
                    else if (employee.PaidLeaveBalance == 0.5M)
                    {
                        employee.PaidLeaveBalance = 0;
                    }

                    employee.UpdatedDate = DateTime.Now;
                    employee.ModifiedDate = DateTime.Now;

                    _context.SaveChanges();
                }
                // =====================================================
                // HALF DAY (HD)
                // =====================================================
                else if (attendance.Status == "HD")
                {
                    // Half Day attendance saved.

                    attendance.UpdatedDate = DateTime.Now;
                    attendance.ModifiedDate = DateTime.Now;

                    _context.EmployeeAttendance.Update(attendance);
                    _context.SaveChanges();

                    TempData["Success"] = "Half Day attendance marked successfully.";
                }

                // =====================================================
                // LEAVE WITHOUT PAY (LWP)
                // =====================================================
                else if (attendance.Status == "LWP")
                {
                    attendance.UpdatedDate = DateTime.Now;
                    attendance.ModifiedDate = DateTime.Now;

                    _context.EmployeeAttendance.Update(attendance);
                    _context.SaveChanges();

                    TempData["Success"] = "Leave Without Pay marked successfully.";
                }

                // =====================================================
                // UNPLANNED LEAVE WITHOUT PAY (ULWP)
                // =====================================================
                else if (attendance.Status == "ULWP")
                {
                    attendance.UpdatedDate = DateTime.Now;
                    attendance.ModifiedDate = DateTime.Now;

                    _context.EmployeeAttendance.Update(attendance);
                    _context.SaveChanges();

                    TempData["Success"] = "Unplanned Leave marked successfully.";
                }


                // =====================================================
                // SANDWICH LEAVE
                // =====================================================
                else if (attendance.Status == "Sandwich")
                {
                    ApplySandwichLeave(employee.EmployeeId, attendance.AttendanceDate);
                }


                // =====================================================
                // ABSCOND
                // =====================================================
                else if (attendance.Status == "Abscond")
                {
                    ApplyRemainingMonthStatus(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Abscond");
                }

                // =====================================================
                // TERMINATED
                // =====================================================
                else if (attendance.Status == "Terminated")
                {
                    ApplyRemainingMonthStatus(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Terminated");
                }

                TempData["Success"] = "Attendance saved successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }

        private void ApplySandwichLeave(int employeeId, DateOnly leaveDate)
        {
            List<DateOnly> dates = new List<DateOnly>();

            // Friday Leave
            if (leaveDate.DayOfWeek == DayOfWeek.Friday)
            {
                dates.Add(leaveDate);
                dates.Add(leaveDate.AddDays(1)); // Saturday
                dates.Add(leaveDate.AddDays(2)); // Sunday
            }

            // Monday Leave
            else if (leaveDate.DayOfWeek == DayOfWeek.Monday)
            {
                dates.Add(leaveDate.AddDays(-2)); // Saturday
                dates.Add(leaveDate.AddDays(-1)); // Sunday
                dates.Add(leaveDate);             // Monday
            }
            else
            {
                return;
            }

            foreach (var day in dates)
            {
                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate == day &&
                    !x.IsDeleted);

                if (!exists)
                {
                    _context.EmployeeAttendance.Add(new EmployeeAttendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = day,
                        Status = "Sandwich",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        IsDeleted = false
                    });
                }
            }

            _context.SaveChanges();
        }

        private void ApplyRemainingMonthStatus(int employeeId,
                                      DateOnly startDate,
                                      string status)
        {
            DateOnly lastDay = new DateOnly(
                startDate.Year,
                startDate.Month,
                DateTime.DaysInMonth(startDate.Year, startDate.Month));

            for (DateOnly day = startDate; day <= lastDay; day = day.AddDays(1))
            {
                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate == day &&
                    !x.IsDeleted);

                if (!exists)
                {
                    _context.EmployeeAttendance.Add(new EmployeeAttendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = day,
                        Status = status,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        IsDeleted = false
                    });
                }
            }

            _context.SaveChanges();
        }

        // Export to the excel file method 

        public IActionResult ExportEmployeesToExcel()
        {
            var employees = _context.MasterEmployee
                            .Where(x => !x.IsDeleted)
                            .ToList();

            ExcelPackage.License.SetNonCommercialPersonal("Admin");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Employees");

                // Headers

                ws.Cells[1, 1].Value = "Employee Code";
                ws.Cells[1, 2].Value = "First Name";
                ws.Cells[1, 3].Value = "Last Name";
                ws.Cells[1, 4].Value = "Full Name";
                ws.Cells[1, 5].Value = "Email";
                ws.Cells[1, 6].Value = "Gender";
                ws.Cells[1, 7].Value = "DOB";
                ws.Cells[1, 8].Value = "Blood Group";
                ws.Cells[1, 9].Value = "Marital Status";
                ws.Cells[1, 10].Value = "Mobile";
                ws.Cells[1, 11].Value = "Alternate Mobile";
                ws.Cells[1, 12].Value = "Emergency Contact Name";
                ws.Cells[1, 13].Value = "Emergency Contact";
                ws.Cells[1, 14].Value = "Address";
                ws.Cells[1, 15].Value = "City";
                ws.Cells[1, 16].Value = "State";
                ws.Cells[1, 17].Value = "Country";
                ws.Cells[1, 18].Value = "Pincode";
                ws.Cells[1, 19].Value = "Department";
                ws.Cells[1, 20].Value = "Designation";
                ws.Cells[1, 21].Value = "Joining Date";
                ws.Cells[1, 22].Value = "Employment Type";
                ws.Cells[1, 23].Value = "Reporting Manager";
                ws.Cells[1, 24].Value = "Work Location";
                ws.Cells[1, 25].Value = "Shift";
                ws.Cells[1, 26].Value = "Salary";
                ws.Cells[1, 27].Value = "Aadhaar";
                ws.Cells[1, 28].Value = "PAN";
                ws.Cells[1, 29].Value = "Passport";
                ws.Cells[1, 30].Value = "Bank";
                ws.Cells[1, 31].Value = "Account Number";
                ws.Cells[1, 32].Value = "IFSC";
                ws.Cells[1, 33].Value = "Status";
                ws.Cells[1, 34].Value = "Created Date";
                ws.Cells[1, 35].Value = "Updated Date";

                using (var range = ws.Cells[1, 1, 1, 35])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.DarkBlue);
                    range.Style.Font.Color.SetColor(Color.White);
                }

                int row = 2;

                foreach (var emp in employees)
                {
                    ws.Cells[row, 1].Value = emp.EmployeeCode;
                    ws.Cells[row, 4].Value = emp.FullName;
                    ws.Cells[row, 5].Value = emp.Email;
                    ws.Cells[row, 6].Value = emp.Gender;
                    ws.Cells[row, 7].Value = emp.DateOfBirth.ToString("dd-MM-yyyy");
                    ws.Cells[row, 8].Value = emp.BloodGroup;
                    ws.Cells[row, 9].Value = emp.MaritalStatus;
                    ws.Cells[row, 10].Value = emp.MobileNumber;
                    ws.Cells[row, 14].Value = emp.AddressLine1;
                    ws.Cells[row, 18].Value = emp.Pincode;
                    ws.Cells[row, 19].Value = emp.Department;
                    ws.Cells[row, 20].Value = emp.Designation;
                    ws.Cells[row, 21].Value = emp.JoiningDate.ToString("dd-MM-yyyy");
                    ws.Cells[row, 22].Value = emp.EmploymentType;
                    ws.Cells[row, 23].Value = emp.ReportingManager;
                    ws.Cells[row, 25].Value = emp.Shift;
                    ws.Cells[row, 26].Value = emp.Salary;
                    ws.Cells[row, 27].Value = emp.AadhaarNumber;
                    ws.Cells[row, 28].Value = emp.PANNumber;
                    ws.Cells[row, 30].Value = emp.BankName;
                    ws.Cells[row, 31].Value = emp.AccountNumber;
                    ws.Cells[row, 32].Value = emp.IFSCCode;
                    ws.Cells[row, 33].Value = emp.EmployeeStatus;
                    ws.Cells[row, 34].Value = emp.CreatedDate.ToString("dd-MM-yyyy");
                    ws.Cells[row, 35].Value = emp.UpdatedDate.ToString("dd-MM-yyyy");

                    row++;
                }

                ws.Cells.AutoFitColumns();

                byte[] file = package.GetAsByteArray();

                return File(
                    file,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"EmployeeMaster_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
        }

        // Attendances Methods 

        public IActionResult ViewAttendance()
        {
            AttendanceReportViewModel model = new AttendanceReportViewModel
            {

                FromDate = DateOnly.FromDateTime(DateTime.Today),
                ToDate = DateOnly.FromDateTime(DateTime.Today)
            };



            model.EmployeeList = _context.MasterEmployee
                                         .Where(x => !x.IsDeleted)
                                         .OrderBy(x => x.FullName)
                                         .ToList();


            model.AttendanceList = new List<EmployeeAttendance>();

            return View(model);
        }

        [HttpPost]
        public IActionResult ViewAttendance(AttendanceReportViewModel model)
        {
            try
            {
                // ==========================================
                // DATE VALIDATION
                // ==========================================
                if (model.FromDate > model.ToDate)
                {
                    TempData["Error"] = "From Date cannot be greater than To Date.";
                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // LOAD EMPLOYEE LIST
                // ==========================================
                model.EmployeeList = _context.MasterEmployee
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.FullName)
                    .ToList();

                // ==========================================
                // LOAD ATTENDANCE
                // ==========================================

                if (model.EmployeeId == 0)
                {
                    // ALL EMPLOYEES
                    model.AttendanceList = _context.EmployeeAttendance
                        .Where(x =>
                            x.AttendanceDate >= model.FromDate &&
                            x.AttendanceDate <= model.ToDate &&
                            !x.IsDeleted)
                        .OrderBy(x => x.AttendanceDate)
                        .ToList();
                }
                else
                {
                    // SPECIFIC EMPLOYEE
                    model.AttendanceList = _context.EmployeeAttendance
                        .Where(x =>
                            x.EmployeeId == model.EmployeeId &&
                            x.AttendanceDate >= model.FromDate &&
                            x.AttendanceDate <= model.ToDate &&
                            !x.IsDeleted)
                        .OrderBy(x => x.AttendanceDate)
                        .ToList();
                }

                // ==========================================
                // LOAD EMPLOYEE DETAILS
                // ==========================================
                var employee = _context.MasterEmployee
                    .FirstOrDefault(x => x.EmployeeId == model.EmployeeId);

                if (employee != null)
                {
                    model.MonthlySalary = employee.Salary;
                }

                // ==========================================
                // CALENDAR DAYS (30 / 31 / 28 / 29)
                // ==========================================
                model.TotalDays = DateTime.DaysInMonth(model.FromDate.Year, model.FromDate.Month);

                // ==========================================
                // HOLIDAY COUNT
                // First 4 Saturdays + All Sundays
                // ==========================================
                int saturdayCount = 0;
                int saturdayNumber = 0;
                int sundayCount = 0;

                for (DateOnly date = model.FromDate;
                     date <= model.ToDate;
                     date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Saturday)
                    {
                        saturdayNumber++;

                        if (saturdayNumber <= 4)
                            saturdayCount++;
                    }

                    if (date.DayOfWeek == DayOfWeek.Sunday)
                        sundayCount++;
                }

                model.SaturdayCount = saturdayCount;
                model.SundayCount = sundayCount;
                model.HolidayCount = saturdayCount + sundayCount;

                // ==========================================
                // WORKING DAYS
                // ==========================================
                model.WorkingDays = model.TotalDays - model.HolidayCount;

                // ==========================================
                // RESET COUNTERS
                // ==========================================
                model.PresentDays = 0;
                model.PaidLeaveCount = 0;
                model.HalfDayCount = 0;
                model.LWPCount = 0;
                model.ULWPCount = 0;
                model.SandwichCount = 0;
                model.AbscondCount = 0;
                model.TerminatedCount = 0;

                // ==========================================
                // ATTENDANCE STATUS COUNT
                // ==========================================
                foreach (var attendance in model.AttendanceList)
                {
                    switch (attendance.Status)
                    {
                        case "Present":
                            model.PresentDays++;
                            break;

                        case "PL":
                            model.PaidLeaveCount++;
                            break;

                        case "HD":
                            model.HalfDayCount++;
                            break;

                        case "LWP":
                            model.LWPCount++;
                            break;

                        case "ULWP":
                            model.ULWPCount++;
                            break;

                        case "Sandwich":
                            model.SandwichCount++;
                            break;

                        case "Abscond":
                            model.AbscondCount++;
                            break;

                        case "Terminated":
                            model.TerminatedCount++;
                            break;
                    }
                }

                // ==========================================
                // ATTENDANCE DAYS
                // PL counts as attendance.
                // HD counts as 0.5 attendance.
                // ==========================================
                model.AttendanceDays =
                      model.PresentDays
                    + model.PaidLeaveCount
                    + (model.HalfDayCount * 0.5M);

                // ==========================================
                // ATTENDANCE %
                // ==========================================
                if (model.TotalDays > 0)
                {
                    model.AttendancePercentage = Math.Round(
                        (double)(model.AttendanceDays / model.TotalDays) * 100,
                        2);
                }

                // ==========================================
                // SALARY CALCULATION
                // Calendar Days (30 / 31)
                // ==========================================
                if (model.TotalDays > 0)
                {
                    model.PerDaySalary =
                        Math.Round(model.MonthlySalary / model.TotalDays, 2);

                    decimal deduction = 0;

                    // Half Day
                    deduction += model.HalfDayCount *
                                 (model.PerDaySalary * 0.5M);

                    // LWP
                    deduction += model.LWPCount *
                                 model.PerDaySalary;

                    // ULWP
                    deduction += model.ULWPCount *
                                 model.PerDaySalary;

                    // Sandwich Leave
                    deduction += model.SandwichCount *
                                 (model.PerDaySalary * 3);

                    // Abscond
                    deduction += model.AbscondCount *
                                 model.PerDaySalary;

                    // Terminated
                    deduction += model.TerminatedCount *
                                 model.PerDaySalary;

                    model.SalaryDeduction =
                        Math.Round(deduction, 2);

                    model.NetSalary =
                        model.MonthlySalary - model.SalaryDeduction;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ViewAttendance");
            }
        }


        [HttpPost]
        public IActionResult ExportAttendanceExcel(AttendanceReportViewModel model)
        {
            try
            {
                // ==========================================
                // ROLE VALIDATION
                // ==========================================
                int? roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId != 5 && roleId != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // DATE VALIDATION
                // ==========================================
                if (model.FromDate > model.ToDate)
                {
                    TempData["Error"] = "From Date cannot be greater than To Date.";
                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // EMPLOYEE LIST
                // ==========================================
                var employeesQuery = _context.MasterEmployee
                    .Where(x => !x.IsDeleted);

                // EmployeeId = 0 means ALL Employees
                if (model.EmployeeId != 0)
                {
                    employeesQuery = employeesQuery
                        .Where(x => x.EmployeeId == model.EmployeeId);
                }

                var employees = employeesQuery
                    .OrderBy(x => x.EmployeeCode)
                    .ToList();

                if (!employees.Any())
                {
                    TempData["Error"] = "No employee found.";
                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // ATTENDANCE RECORDS
                // ==========================================
                var attendanceList = _context.EmployeeAttendance
                    .Where(x =>
                        x.AttendanceDate >= model.FromDate &&
                        x.AttendanceDate <= model.ToDate &&
                        !x.IsDeleted)
                    .ToList();

                // ==========================================
                // APPROVED LEAVE REQUESTS
                // ==========================================
                var leaveList = _context.LeaveRequests
                    .Where(x =>
                        x.Status == "Approved" &&
                        x.FromDate <= model.ToDate &&
                        x.ToDate >= model.FromDate)
                    .ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendance Report");

                    // ==========================================
                    // COMPANY TITLE
                    // ==========================================
                    worksheet.Cell("A1").Value = "MARS TECH SOLUTIONS";
                    worksheet.Range("A1:AO1").Merge();
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 18;
                    worksheet.Cell("A1").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    worksheet.Cell("A2").Value = "Employee Attendance & Salary Report";
                    worksheet.Range("A2:AO2").Merge();
                    worksheet.Cell("A2").Style.Font.Bold = true;
                    worksheet.Cell("A2").Style.Font.FontSize = 14;
                    worksheet.Cell("A2").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    worksheet.Cell("A3").Value =
                        $"From : {model.FromDate:dd-MMM-yyyy}  To : {model.ToDate:dd-MMM-yyyy}";
                    worksheet.Range("A3:AO3").Merge();
                    worksheet.Cell("A3").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    int headerRow = 5;

                    worksheet.Cell(headerRow, 1).Value = "Employee Code";
                    worksheet.Cell(headerRow, 2).Value = "Employee Name";

                    // ==========================================
                    // DATE HEADERS
                    // ==========================================
                    int column = 3;
                    DateOnly currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        worksheet.Cell(headerRow, column).Value =
                            currentDate.ToString("dd-MMM");

                        worksheet.Cell(headerRow, column).Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                        if (currentDate.DayOfWeek == DayOfWeek.Saturday ||
                            currentDate.DayOfWeek == DayOfWeek.Sunday)
                        {
                            worksheet.Cell(headerRow, column)
                                .Style.Fill.BackgroundColor = XLColor.Yellow;
                        }

                        column++;
                        currentDate = currentDate.AddDays(1);
                    }

                    // ==========================================
                    // SUMMARY COLUMNS
                    // ==========================================
                    worksheet.Cell(headerRow, column++).Value = "Calendar Days";
                    worksheet.Cell(headerRow, column++).Value = "Present";
                    worksheet.Cell(headerRow, column++).Value = "PL";
                    worksheet.Cell(headerRow, column++).Value = "Holiday";
                    worksheet.Cell(headerRow, column++).Value = "HD";
                    worksheet.Cell(headerRow, column++).Value = "LWP";
                    worksheet.Cell(headerRow, column++).Value = "ULWP";
                    worksheet.Cell(headerRow, column++).Value = "Sandwich";
                    worksheet.Cell(headerRow, column++).Value = "Abscond";
                    worksheet.Cell(headerRow, column++).Value = "Terminated";
                    worksheet.Cell(headerRow, column++).Value = "Attendance %";
                    worksheet.Cell(headerRow, column++).Value = "Monthly Salary";
                    worksheet.Cell(headerRow, column++).Value = "Salary Deduction";
                    worksheet.Cell(headerRow, column++).Value = "Net Salary";

                    // ==========================================
                    // EMPLOYEE LOOP
                    // ==========================================
                    int row = headerRow + 1;

                    foreach (var employee in employees)
                    {
                        worksheet.Cell(row, 1).Value = employee.EmployeeCode;
                        worksheet.Cell(row, 2).Value = employee.FullName;

                        int present = 0;
                        int paidLeave = 0;
                        int holiday = 0;
                        int halfDay = 0;
                        int lwp = 0;
                        int ulwp = 0;
                        int sandwich = 0;
                        int abscond = 0;
                        int terminated = 0;
                        int wfh = 0;
                        int emergency = 0;

                        column = 3;
                        currentDate = model.FromDate;

                        while (currentDate <= model.ToDate)
                        {
                            bool isHoliday = false;

                            if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                            {
                                isHoliday = true;
                            }
                            else if (currentDate.DayOfWeek == DayOfWeek.Saturday)
                            {
                                int saturdayNumber = 0;

                                for (DateOnly d = new DateOnly(currentDate.Year, currentDate.Month, 1);
                                     d <= currentDate;
                                     d = d.AddDays(1))
                                {
                                    if (d.DayOfWeek == DayOfWeek.Saturday)
                                        saturdayNumber++;
                                }

                                if (saturdayNumber <= 4)
                                    isHoliday = true;
                            }

                            var attendance = attendanceList.FirstOrDefault(x =>
                                x.EmployeeId == employee.EmployeeId &&
                                x.AttendanceDate == currentDate);

                            var leave = leaveList.FirstOrDefault(x =>
                                x.UserId == employee.EmployeeId &&
                                currentDate >= x.FromDate &&
                                currentDate <= x.ToDate);

                            string value = "";

                            if (isHoliday)
                            {
                                value = "H";
                                holiday++;
                            }
                            else if (attendance != null)
                            {
                                switch (attendance.Status)
                                {
                                    case "Present":
                                        value = "P";
                                        present++;
                                        break;

                                    case "PL":
                                        value = "PL";
                                        paidLeave++;
                                        break;

                                    case "HD":
                                        value = "HD";
                                        halfDay++;
                                        break;

                                    case "LWP":
                                        value = "LWP";
                                        lwp++;
                                        break;

                                    case "ULWP":
                                        value = "ULWP";
                                        ulwp++;
                                        break;

                                    case "Sandwich":
                                        value = "SW";
                                        sandwich++;
                                        break;

                                    case "Abscond":
                                        value = "ABS";
                                        abscond++;
                                        break;

                                    case "Terminated":
                                        value = "TERM";
                                        terminated++;
                                        break;

                                    default:
                                        value = "A";
                                        lwp++;
                                        break;
                                }
                            }
                            else if (leave != null)
                            {
                                switch (leave.LeaveTypeId)
                                {
                                    case 1:
                                        value = "PL";
                                        paidLeave++;
                                        break;

                                    case 2:
                                        value = "PL";
                                        paidLeave++;
                                        break;

                                    case 3:
                                        value = "WFH";
                                        wfh++;
                                        break;

                                    case 4:
                                        value = "HD";
                                        halfDay++;
                                        break;

                                    case 5:
                                        value = "EL";
                                        emergency++;
                                        break;
                                }
                            }
                            else
                            {
                                value = "";
                            }

                            worksheet.Cell(row, column).Value = value;

                            var cell = worksheet.Cell(row, column);

                            switch (value)
                            {
                                case "P":
                                    cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                                    break;

                                case "PL":
                                    cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                                    break;

                                case "HD":
                                    cell.Style.Fill.BackgroundColor = XLColor.Plum;
                                    break;

                                case "LWP":
                                    cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                                    break;

                                case "ULWP":
                                    cell.Style.Fill.BackgroundColor = XLColor.Red;
                                    cell.Style.Font.FontColor = XLColor.White;
                                    break;

                                case "SW":
                                    cell.Style.Fill.BackgroundColor = XLColor.Gold;
                                    break;

                                case "ABS":
                                    cell.Style.Fill.BackgroundColor = XLColor.DarkRed;
                                    cell.Style.Font.FontColor = XLColor.White;
                                    break;

                                case "TERM":
                                    cell.Style.Fill.BackgroundColor = XLColor.Gray;
                                    cell.Style.Font.FontColor = XLColor.White;
                                    break;

                                case "WFH":
                                    cell.Style.Fill.BackgroundColor = XLColor.LightCyan;
                                    break;

                                case "EL":
                                    cell.Style.Fill.BackgroundColor = XLColor.LightSalmon;
                                    break;

                                case "H":
                                    cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                                    break;
                            }

                            cell.Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Center;

                            column++;
                            currentDate = currentDate.AddDays(1);
                        }

                        // ==========================================
                        // SALARY CALCULATION
                        // ==========================================
                        int calendarDays = DateTime.DaysInMonth(
                            model.FromDate.Year,
                            model.FromDate.Month);

                        decimal monthlySalary = employee.Salary;

                        decimal perDaySalary =
                            Math.Round(monthlySalary / calendarDays, 2);

                        decimal attendanceDays =
                            present +
                            paidLeave +
                            wfh +
                            emergency +
                            (halfDay * 0.5M);

                        decimal attendancePercentage =
                            Math.Round((attendanceDays / calendarDays) * 100, 2);

                        decimal hdDeduction =
                            halfDay * (perDaySalary * 0.5M);

                        decimal lwpDeduction =
                            lwp * perDaySalary;

                        decimal ulwpDeduction =
                            ulwp * perDaySalary;

                        decimal sandwichDeduction =
                            sandwich * (perDaySalary * 3M);

                        decimal abscondDeduction =
                            abscond * perDaySalary;

                        decimal terminatedDeduction =
                            terminated * perDaySalary;

                        decimal totalDeduction =
                            hdDeduction +
                            lwpDeduction +
                            ulwpDeduction +
                            sandwichDeduction +
                            abscondDeduction +
                            terminatedDeduction;

                        decimal netSalary =
                            monthlySalary - totalDeduction;

                        // ==========================================
                        // SUMMARY COLUMNS
                        // ==========================================
                        worksheet.Cell(row, column++).Value = calendarDays;
                        worksheet.Cell(row, column++).Value = present;
                        worksheet.Cell(row, column++).Value = paidLeave;
                        worksheet.Cell(row, column++).Value = holiday;
                        worksheet.Cell(row, column++).Value = halfDay;
                        worksheet.Cell(row, column++).Value = lwp;
                        worksheet.Cell(row, column++).Value = ulwp;
                        worksheet.Cell(row, column++).Value = sandwich;
                        worksheet.Cell(row, column++).Value = abscond;
                        worksheet.Cell(row, column++).Value = terminated;
                        worksheet.Cell(row, column++).Value = attendancePercentage + "%";
                        worksheet.Cell(row, column++).Value = monthlySalary;
                        worksheet.Cell(row, column++).Value = totalDeduction;
                        worksheet.Cell(row, column++).Value = netSalary;

                        row++;
                    }

                    // ==========================================
                    // HEADER FORMAT
                    // ==========================================
                    var headerRange = worksheet.Range(
                        headerRow,
                        1,
                        headerRow,
                        worksheet.LastColumnUsed().ColumnNumber());

                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.SteelBlue;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;

                    // ==========================================
                    // BORDERS
                    // ==========================================
                    var usedRange = worksheet.RangeUsed();

                    if (usedRange != null)
                    {
                        usedRange.Style.Border.OutsideBorder =
                            XLBorderStyleValues.Thin;

                        usedRange.Style.Border.InsideBorder =
                            XLBorderStyleValues.Thin;
                    }

                    // ==========================================
                    // CENTER ALIGN ALL ATTENDANCE & SUMMARY COLUMNS
                    // ==========================================

                    // Employee Code Center
                    worksheet.Column(1).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // Employee Name Left
                    worksheet.Column(2).Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Left;

                    // Attendance Date Columns (P, A, PL, H, etc.)
                    worksheet.Range(
                        headerRow + 1,
                        3,
                        row - 1,
                        2 + model.TotalDays)
                        .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // Summary Columns (Working Days, Present, Salary, etc.)
                    worksheet.Range(
                        headerRow + 1,
                        3 + model.TotalDays,
                        row - 1,
                        worksheet.LastColumnUsed().ColumnNumber())
                        .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // Vertical Alignment
                    worksheet.RangeUsed().Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;

                    worksheet.Columns().AdjustToContents();
                    worksheet.Column(2).Width = 28;

                    worksheet.SheetView.FreezeRows(headerRow);
                    worksheet.SheetView.FreezeColumns(2);

                    // ==========================================
                    // DOWNLOAD FILE
                    // ==========================================
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        string fileName =
                            "Attendance_Report_" +
                            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                            ".xlsx";

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("ViewAttendance");
            }
        }

        // Login trackerr 


        public IActionResult LoginTracker()
        {
            try
            {
                // Allow only HR (RoleId = 5) and Admin (RoleId = 3)
                if (HttpContext.Session.GetInt32("RoleId") != 5 &&
                    HttpContext.Session.GetInt32("RoleId") != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                LoginTrackerViewModel model = new LoginTrackerViewModel();

                model.FromDate = DateOnly.FromDateTime(DateTime.Today);
                model.ToDate = DateOnly.FromDateTime(DateTime.Today);

                model.EmployeeList = _context.MasterEmployee
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.FullName)
                    .ToList();

                model.LoginList = new List<EmployeeLoginTracker>();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Something went wrong while loading the Login Tracker.";

                // Optional: Log the actual exception for debugging
                Console.WriteLine(ex.Message);

                return RedirectToAction("HRDashboard", "HR");
            }
        }

        [HttpPost]
        public IActionResult LoginTracker(LoginTrackerViewModel model)
        {
            model.EmployeeList = _context.MasterEmployee
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .ToList();

            var query = _context.EmployeeLoginTracker
                .Where(x =>
                    x.LoginDate >= model.FromDate &&
                    x.LoginDate <= model.ToDate &&
                    !x.IsDeleted);

            if (model.EmployeeId != 0)
            {
                query = query.Where(x => x.EmployeeId == model.EmployeeId);
            }

            model.LoginList = query
                .OrderByDescending(x => x.LoginDate)
                .ThenBy(x => x.FullName)
                .ToList();

            // Summary Cards

            model.TotalPresentDays = model.LoginList.Count();

            model.TotalWorkingHours = model.LoginList
                .Sum(x => x.TotalWorkingHours ?? 0);

            model.NotSignedOutCount = model.LoginList
                .Count(x => x.SignOutTime == null);

            // Late Login (After 9:15 AM)

            TimeOnly officeTime = new TimeOnly(9, 15);

            model.LateLoginCount = model.LoginList.Count(x => TimeOnly.FromDateTime(x.SignInTime.Value) > officeTime);

            if (model.TotalPresentDays > 0)
            {
                model.AverageWorkingHours = Math.Round(model.TotalWorkingHours / model.TotalPresentDays, 2);
            }

            return View(model);
        }

        // Excel Report login tracker 

        [HttpPost]
        public IActionResult ExportLoginTrackerExcel(LoginTrackerViewModel model)
        {
            try
            {
                // ===================================================
                // ROLE VALIDATION
                // ===================================================
                int? roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId != 5 && roleId != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ===================================================
                // DATE VALIDATION
                // ===================================================
                if (model.FromDate > model.ToDate)
                {
                    TempData["Error"] = "From Date cannot be greater than To Date.";
                    return RedirectToAction("LoginTracker");
                }

                // ===================================================
                // GET EMPLOYEES
                // EmployeeId = 0 means ALL Employees
                // ===================================================
                var employeesQuery = _context.MasterEmployee
                    .Where(x => !x.IsDeleted);

                if (model.EmployeeId != 0)
                {
                    employeesQuery = employeesQuery
                        .Where(x => x.EmployeeId == model.EmployeeId);
                }

                var employees = employeesQuery
                    .OrderBy(x => x.EmployeeCode)
                    .ToList();

                // ===================================================
                // GET LOGIN TRACKER DATA
                // ===================================================
                var loginList = _context.EmployeeLoginTracker
                    .Where(x =>
                        x.LoginDate >= model.FromDate &&
                        x.LoginDate <= model.ToDate &&
                        !x.IsDeleted)
                    .ToList();

                // ===================================================
                // CREATE EXCEL
                // ===================================================
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Login Tracker");

                    // ===================================================
                    // COMPANY HEADER
                    // ===================================================

                    worksheet.Cell("A1").Value = "MARS TECH SOLUTIONS";
                    worksheet.Range("A1:I1").Merge();
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 18;
                    worksheet.Cell("A1").Style.Fill.BackgroundColor = XLColor.DarkBlue;
                    worksheet.Cell("A1").Style.Font.FontColor = XLColor.White;
                    worksheet.Cell("A1").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    worksheet.Cell("A2").Value = "Employee Login Tracker Report";
                    worksheet.Range("A2:I2").Merge();
                    worksheet.Cell("A2").Style.Font.Bold = true;
                    worksheet.Cell("A2").Style.Font.FontSize = 14;
                    worksheet.Cell("A2").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    worksheet.Cell("A3").Value =
                        $"From : {model.FromDate:dd-MMM-yyyy}    To : {model.ToDate:dd-MMM-yyyy}";
                    worksheet.Range("A3:I3").Merge();
                    worksheet.Cell("A3").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    // ===================================================
                    // HEADER ROW
                    // ===================================================

                    int headerRow = 5;

                    worksheet.Cell(headerRow, 1).Value = "Employee Code";
                    worksheet.Cell(headerRow, 2).Value = "Employee Name";

                    int column = 3;
                    DateOnly currentDate = model.FromDate;

                    while (currentDate <= model.ToDate)
                    {
                        worksheet.Cell(headerRow, column).Value =
                            currentDate.ToString("dd-MMM");

                        worksheet.Cell(headerRow, column)
                            .Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                        // Weekend Header Color
                        if (currentDate.DayOfWeek == DayOfWeek.Saturday ||
                            currentDate.DayOfWeek == DayOfWeek.Sunday)
                        {
                            worksheet.Cell(headerRow, column)
                                .Style.Fill.BackgroundColor = XLColor.Yellow;
                        }

                        column++;
                        currentDate = currentDate.AddDays(1);
                    }

                    // Summary Columns
                    worksheet.Cell(headerRow, column++).Value = "Total Present";
                    worksheet.Cell(headerRow, column++).Value = "Total Hours";
                    worksheet.Cell(headerRow, column++).Value = "Late Login";
                    worksheet.Cell(headerRow, column++).Value = "Average Hours";

                    // ===================================================
                    // EMPLOYEE LOGIN DATA
                    // ===================================================

                    int row = headerRow + 1;

                    foreach (var employee in employees)
                    {
                        worksheet.Cell(row, 1).Value = employee.EmployeeCode;
                        worksheet.Cell(row, 2).Value = employee.FullName;

                        int presentDays = 0;
                        decimal totalHours = 0;
                        int lateLoginDays = 0;

                        column = 3;
                        currentDate = model.FromDate;

                        while (currentDate <= model.ToDate)
                        {
                            var login = loginList.FirstOrDefault(x =>
                                x.EmployeeId == employee.EmployeeId &&
                                x.LoginDate == currentDate);

                            if (login != null)
                            {
                                presentDays++;

                                string signIn =
                                    login.SignInTime?.ToString("hh:mm tt") ?? "--";

                                string signOut =
                                    login.SignOutTime?.ToString("hh:mm tt") ?? "--";

                                worksheet.Cell(row, column).Value =
                                    $"{signIn}\n{signOut}";

                                worksheet.Cell(row, column)
                                    .Style.Alignment.WrapText = true;

                                // Completed Day
                                if (login.SignOutTime != null)
                                {
                                    worksheet.Cell(row, column)
                                        .Style.Fill.BackgroundColor = XLColor.LightGreen;
                                }
                                else
                                {
                                    worksheet.Cell(row, column)
                                        .Style.Fill.BackgroundColor = XLColor.LightPink;
                                }

                                // Late Login (After 9:15 AM)
                                if (login.SignInTime.HasValue &&
                                    login.SignInTime.Value.TimeOfDay >
                                    new TimeSpan(9, 15, 0))
                                {
                                    lateLoginDays++;
                                }

                                totalHours += login.TotalWorkingHours ?? 0;
                            }
                            else
                            {
                                worksheet.Cell(row, column).Value = "Absent";

                                worksheet.Cell(row, column)
                                    .Style.Fill.BackgroundColor = XLColor.LightGray;
                            }

                            worksheet.Cell(row, column)
                                .Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Center;

                            worksheet.Cell(row, column)
                                .Style.Alignment.Vertical =
                                XLAlignmentVerticalValues.Center;

                            column++;
                            currentDate = currentDate.AddDays(1);
                        }

                        // ===================================================
                        // SUMMARY VALUES
                        // ===================================================

                        worksheet.Cell(row, column++).Value = presentDays;
                        worksheet.Cell(row, column++).Value = Math.Round(totalHours, 2);
                        worksheet.Cell(row, column++).Value = lateLoginDays;

                        worksheet.Cell(row, column).Value =
                            presentDays == 0
                                ? 0
                                : Math.Round(totalHours / presentDays, 2);

                        row++;
                    }

                    // ===================================================
                    // HEADER STYLE
                    // ===================================================

                    var headerRange = worksheet.Range(
                        headerRow,
                        1,
                        headerRow,
                        worksheet.LastColumnUsed().ColumnNumber());

                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Fill.BackgroundColor = XLColor.SteelBlue;
                    headerRange.Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;

                    // ===================================================
                    // BORDER
                    // ===================================================

                    var usedRange = worksheet.RangeUsed();

                    if (usedRange != null)
                    {
                        usedRange.Style.Border.OutsideBorder =
                            XLBorderStyleValues.Thin;

                        usedRange.Style.Border.InsideBorder =
                            XLBorderStyleValues.Thin;
                    }

                    // ===================================================
                    // COLUMN WIDTH
                    // ===================================================

                    worksheet.Columns().AdjustToContents();

                    worksheet.Column(2).Width = 28;

                    // Date columns width
                    for (int i = 3; i <= worksheet.LastColumnUsed().ColumnNumber(); i++)
                    {
                        worksheet.Column(i).Width = 18;
                    }

                    worksheet.SheetView.FreezeRows(headerRow);
                    worksheet.SheetView.FreezeColumns(2);

                    // ===================================================
                    // DOWNLOAD EXCEL
                    // ===================================================

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        string fileName =
                            "Employee_Login_Tracker_" +
                            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                            ".xlsx";

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("LoginTracker");
            }
        }


        // ==========================================
        // EDIT ATTENDANCE
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditAttendance(int attendanceId)
        {
            try
            {
                // Only HR can edit attendance
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // FIND EXISTING ATTENDANCE
                // ==========================================

                var attendance = await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.AttendanceId == attendanceId &&
                        !x.IsDeleted);

                if (attendance == null)
                {
                    TempData["Error"] = "Attendance record not found.";

                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // FIND EMPLOYEE
                // ==========================================

                var employee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == attendance.EmployeeId &&
                        !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";

                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // CREATE DASHBOARD MODEL
                // ==========================================

                HRDashboardViewModel model =
                    new HRDashboardViewModel();

                // Existing employee
                model.MasterEmployee = employee;

                // Existing attendance
                model.EmployeeAttendance = attendance;

                // ==========================================
                // LOAD EMPLOYEE LIST
                // ==========================================

                model.EmployeeList = await _context.MasterEmployee
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.FullName)
                    .ToListAsync();

                // ==========================================
                // EDIT MODE
                // ==========================================

                model.IsAttendanceEdit = true;

                // IMPORTANT:
                // Keep using your existing Dashboard view.
                // This prevents creating a new Attendance view
                // and keeps your existing page structure.
                return View("Dashboard", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("ViewAttendance");
            }
        }


        // ==========================================
        // UPDATE ATTENDANCE
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(
            HRDashboardViewModel model)
        {
            try
            {
                // Only HR can update attendance
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // NULL CHECK
                // ==========================================

                if (model.EmployeeAttendance == null)
                {
                    TempData["Error"] =
                        "Attendance information is missing.";

                    return RedirectToAction("ViewAttendance");
                }

                var attendanceModel =
                    model.EmployeeAttendance;

                // ==========================================
                // FIND EXISTING ATTENDANCE
                // ==========================================

                var attendance =
                    await _context.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.AttendanceId ==
                                attendanceModel.AttendanceId &&
                            !x.IsDeleted);

                if (attendance == null)
                {
                    TempData["Error"] =
                        "Attendance record not found.";

                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // CHECK EMPLOYEE
                // ==========================================

                var employee =
                    await _context.MasterEmployee
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                                attendanceModel.EmployeeId &&
                            !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] =
                        "Employee not found.";

                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // DUPLICATE ATTENDANCE CHECK
                // ==========================================

                bool duplicate =
                    await _context.EmployeeAttendance
                        .AnyAsync(x =>
                            x.AttendanceId !=
                                attendanceModel.AttendanceId &&

                            x.EmployeeId ==
                                attendanceModel.EmployeeId &&

                            x.AttendanceDate ==
                                attendanceModel.AttendanceDate &&

                            !x.IsDeleted);

                if (duplicate)
                {
                    TempData["Error"] =
                        "Attendance already exists for this employee on the selected date.";

                    return RedirectToAction(
                        "EditAttendance",
                        new
                        {
                            attendanceId =
                                attendanceModel.AttendanceId
                        });
                }

                // ==========================================
                // UPDATE EXISTING RECORD
                // ==========================================

                attendance.EmployeeId =
                    attendanceModel.EmployeeId;

                attendance.AttendanceDate =
                    attendanceModel.AttendanceDate;

                attendance.Status =
                    attendanceModel.Status;

                attendance.Remarks =
                    attendanceModel.Remarks;

                // Do NOT change CreatedDate
                attendance.UpdatedDate =
                    DateTime.Now;

                attendance.ModifiedDate =
                    DateTime.Now;

                // ==========================================
                // SAVE CHANGES ASYNC
                // ==========================================

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Attendance updated successfully.";

                // Return to attendance report
                return RedirectToAction("ViewAttendance");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                // Prevent null reference in catch
                if (model?.EmployeeAttendance != null)
                {
                    return RedirectToAction(
                        "EditAttendance",
                        new
                        {
                            attendanceId =
                                model.EmployeeAttendance.AttendanceId
                        });
                }

                return RedirectToAction("ViewAttendance");
            }
        }
    }
}
using Azure.Core;
using ClosedXML.Excel;
using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.Services;
using CRMPortal.ViewModels;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CRMPortal.Controllers
{
    public class HRController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HRController(AppDbContext context,IWebHostEnvironment environment)
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
        public async Task<IActionResult> Employees()
        {
            try
            {
                // ==========================================
                // ONLY HR
                // ==========================================
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // LOAD ALL EMPLOYEES
                //
                // Includes:
                // IsDeleted = 0 Active
                // IsDeleted = 1 Inactive
                // ==========================================
                    var employees = await _context.MasterEmployee
                 .OrderBy(x => x.IsDeleted)
                 .ThenByDescending(x => x.EmployeeId)
                 .ToListAsync();

                // ==========================================
                // SUMMARY COUNTS
                // ==========================================
                ViewBag.TotalEmployees = employees.Count;

                ViewBag.ActiveEmployees =
                    employees.Count(x => !x.IsDeleted);

                ViewBag.InactiveEmployees =
                    employees.Count(x => x.IsDeleted);

                return View(employees);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Something went wrong while loading employees.";

                Console.WriteLine(ex.Message);

                return View(new List<CRMPortal.Models.MasterEmployee>());
            }
        }

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

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(HRDashboardViewModel model)
        {
            try
            {
                // ==========================================
                // ALLOW ONLY HR
                // ==========================================
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = model.MasterEmployee;

                // ==========================================
                // FIND EMPLOYEE
                // ==========================================
                var dbEmployee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employee.EmployeeId);

                if (dbEmployee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // DUPLICATE EMPLOYEE CODE CHECK
                // ==========================================
                bool employeeCodeExists = await _context.MasterEmployee
                    .AnyAsync(x =>
                        x.EmployeeCode == employee.EmployeeCode &&
                        x.EmployeeId != employee.EmployeeId &&
                        !x.IsDeleted);

                if (employeeCodeExists)
                {
                    TempData["Error"] = "Employee Code already exists.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // DUPLICATE EMPLOYEE NAME CHECK
                // ==========================================
                bool employeeNameExists = await _context.MasterEmployee
                    .AnyAsync(x =>
                        x.FullName == employee.FullName &&
                        x.EmployeeId != employee.EmployeeId &&
                        !x.IsDeleted);

                if (employeeNameExists)
                {
                    TempData["Error"] = "Employee Name already exists.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // UPDATE EMPLOYEE DETAILS
                // ==========================================
                dbEmployee.EmployeeCode = employee.EmployeeCode;
                dbEmployee.FullName = employee.FullName;
                dbEmployee.Email = employee.Email;
                dbEmployee.MobileNumber = employee.MobileNumber;
                dbEmployee.EmergencyContactName = employee.EmergencyContactName;
                dbEmployee.EmergencyContact = employee.EmergencyContact;
                dbEmployee.Gender = employee.Gender;
                dbEmployee.DateOfBirth = employee.DateOfBirth;
                dbEmployee.BloodGroup = employee.BloodGroup;
                dbEmployee.MaritalStatus = employee.MaritalStatus;
                dbEmployee.AddressLine1 = employee.AddressLine1;
                dbEmployee.Pincode = employee.Pincode;
                dbEmployee.Department = employee.Department;
                dbEmployee.Designation = employee.Designation;
                dbEmployee.JoiningDate = employee.JoiningDate;

                // ==========================================
                // EMPLOYMENT TYPE
                // ==========================================
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

                // ==========================================
                // EMPLOYEE STATUS
                // ==========================================
                dbEmployee.EmployeeStatus = employee.EmployeeStatus;

                // ==========================================
                // IMPORTANT
                //
                // DO NOT UPDATE IsDeleted HERE.
                //
                // Activate/InActivate will control IsDeleted.
                // ==========================================

                // ==========================================
                // AUDIT FIELDS
                // ==========================================
                dbEmployee.ModifiedDate = DateTime.Now;
                dbEmployee.UpdatedDate = DateTime.Now;

                // ==========================================
                // SAVE CHANGES
                // ==========================================
                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee updated successfully.";

                return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Something went wrong while updating the employee.";

                Console.WriteLine(ex.Message);

                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActivateEmployee(int id)
        {
            try
            {
                // ==========================================
                // ONLY HR CAN ACTIVATE EMPLOYEE
                // ==========================================
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // FIND EMPLOYEE
                // ==========================================
                var employee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // CHECK IF ALREADY ACTIVE
                // ==========================================
                if (!employee.IsDeleted)
                {
                    TempData["Error"] = "Employee is already active.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // INACTIVE -> ACTIVE
                //
                // IsDeleted:
                // 1 -> 0
                // ==========================================
                employee.IsDeleted = false;

                // Keep EmployeeStatus also synchronized
                employee.EmployeeStatus = "Active";

                // ==========================================
                // UPDATE AUDIT FIELDS
                // ==========================================
                employee.UpdatedDate = DateTime.Now;
                employee.ModifiedDate = DateTime.Now;

                // ==========================================
                // SAVE
                // ==========================================
                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee activated successfully.";

                return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Something went wrong while activating the employee.";

                Console.WriteLine(ex.Message);

                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                // ==========================================
                // ONLY HR CAN INACTIVATE
                // ==========================================
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ==========================================
                // FIND EMPLOYEE
                // ==========================================
                var employee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // CHECK IF ALREADY INACTIVE
                // ==========================================
                if (employee.IsDeleted)
                {
                    TempData["Error"] = "Employee is already inactive.";
                    return RedirectToAction("Employees");
                }

                // ==========================================
                // ACTIVE -> INACTIVE
                //
                // IsDeleted:
                // 0 -> 1
                // ==========================================
                employee.IsDeleted = true;

                // Keep EmployeeStatus synchronized
                employee.EmployeeStatus = "Inactive";

                // ==========================================
                // AUDIT FIELDS
                // ==========================================
                employee.UpdatedDate = DateTime.Now;
                employee.ModifiedDate = DateTime.Now;

                // ==========================================
                // SAVE
                // ==========================================
                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee inactivated successfully.";

                return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Something went wrong while inactivating the employee.";

                Console.WriteLine(ex.Message);

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
                // =====================================================
                // ROLE VALIDATION
                // =====================================================

                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var attendance = model.EmployeeAttendance;

                if (attendance == null)
                {
                    TempData["Error"] = "Attendance information is missing.";
                    return RedirectToAction("Dashboard");
                }

                // =====================================================
                // VALIDATE EMPLOYEE
                // =====================================================

                var employee = _context.MasterEmployee
                    .FirstOrDefault(x =>
                        x.EmployeeId == attendance.EmployeeId &&
                        !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Dashboard");
                }

                // =====================================================
                // VALID STATUS
                // =====================================================

                string[] validStatuses =
                {
                    "Present",
                    "PL",
                    "HD",
                    "H",
                    "LWP",
                    "ULWP",
                    "SL",
                    "NCNS",
                    "Abscond",
                    "Terminated"
                };

                if (!validStatuses.Contains(attendance.Status))
                {
                    TempData["Error"] = "Invalid attendance status.";
                    return RedirectToAction("Dashboard");
                }

                // =====================================================
                // SANDWICH VALIDATION
                // =====================================================

                if (attendance.Status == "SL")
                {
                    if (attendance.AttendanceDate.DayOfWeek != DayOfWeek.Friday &&
                        attendance.AttendanceDate.DayOfWeek != DayOfWeek.Monday)
                    {
                        TempData["Error"] =
                            "Sandwich Leave can only be applied on Friday or Monday.";

                        return RedirectToAction("Dashboard");
                    }
                }

                // =====================================================
                // CHECK DUPLICATE MAIN DATE
                // =====================================================

                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == attendance.EmployeeId &&
                    x.AttendanceDate == attendance.AttendanceDate &&
                    !x.IsDeleted);

                if (exists)
                {
                    TempData["Error"] =
                        "Attendance already marked for this employee on the selected date.";

                    return RedirectToAction("Dashboard");
                }

                // =====================================================
                // DATE / STATUS PROCESSING
                // =====================================================

                if (attendance.Status == "SL")
                {
                    // IMPORTANT:
                    // Do NOT add the main attendance first.
                    //
                    // ApplySandwichLeave itself will add:
                    // Friday + Saturday + Sunday
                    // OR
                    // Saturday + Sunday + Monday

                    ApplySandwichLeave(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        attendance.Remarks);
                }
                else if (attendance.Status == "Abscond")
                {
                    // First save Abscond date.
                    AddAttendanceRecord(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Abscond",
                        attendance.Remarks);

                    // Mark remaining month as Abscond.
                    ApplyRemainingMonthStatus(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Abscond");
                }
                else if (attendance.Status == "Terminated")
                {
                    // First save Terminated date.
                    AddAttendanceRecord(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Terminated",
                        attendance.Remarks);

                    // Mark remaining month as Terminated.
                    ApplyRemainingMonthStatus(
                        employee.EmployeeId,
                        attendance.AttendanceDate,
                        "Terminated");
                }
                else
                {
                    // =================================================
                    // NORMAL ATTENDANCE
                    // =================================================

                    attendance.CreatedDate = DateTime.Now;
                    attendance.UpdatedDate = DateTime.Now;
                    attendance.ModifiedDate = DateTime.Now;
                    attendance.IsDeleted = false;

                    _context.EmployeeAttendance.Add(attendance);

                    // =================================================
                    // PAID LEAVE BALANCE
                    // =================================================

                    if (attendance.Status == "PL")
                    {
                        if (employee.PaidLeaveBalance >= 1M)
                        {
                            employee.PaidLeaveBalance -= 1M;
                        }
                        else if (employee.PaidLeaveBalance == 0.5M)
                        {
                            employee.PaidLeaveBalance = 0M;
                        }

                        employee.UpdatedDate = DateTime.Now;
                        employee.ModifiedDate = DateTime.Now;
                    }
                }

                // =====================================================
                // EMPLOYEE UPDATED DATE
                // =====================================================

                employee.UpdatedDate = DateTime.Now;
                employee.ModifiedDate = DateTime.Now;

                // =====================================================
                // SAVE ONCE
                // =====================================================

                _context.SaveChanges();

                TempData["Success"] =
                    "Attendance saved successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }


        [HttpGet]
        public IActionResult ViewAttendance()
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5 &&
                    HttpContext.Session.GetInt32("RoleId") != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                AttendanceReportViewModel model =
                    new AttendanceReportViewModel();

                model.FromDate =
                    new DateOnly(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        1);

                model.ToDate =
                    new DateOnly(
                        DateTime.Today.Year,
                        DateTime.Today.Month,
                        DateTime.DaysInMonth(
                            DateTime.Today.Year,
                            DateTime.Today.Month));

                model.EmployeeList =
                    _context.MasterEmployee
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.FullName)
                        .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult ViewAttendance(AttendanceReportViewModel model)
        {
            try
            {
                // =====================================================
                // ROLE
                // =====================================================

                if (HttpContext.Session.GetInt32("RoleId") != 5 &&
                    HttpContext.Session.GetInt32("RoleId") != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                // =====================================================
                // DATE VALIDATION
                // =====================================================

                if (model.FromDate > model.ToDate)
                {
                    TempData["Error"] =
                        "From Date cannot be greater than To Date.";

                    return RedirectToAction("ViewAttendance");
                }

                if (model.FromDate.Year != model.ToDate.Year ||
                    model.FromDate.Month != model.ToDate.Month)
                {
                    TempData["Error"] =
                        "Please select From Date and To Date within the same month.";

                    return RedirectToAction("ViewAttendance");
                }

                // =====================================================
                // EMPLOYEE LIST
                // =====================================================

                model.EmployeeList =
                    _context.MasterEmployee
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.FullName)
                        .ToList();

                // =====================================================
                // ATTENDANCE LIST
                // =====================================================

                if (model.EmployeeId == 0)
                {
                    model.AttendanceList =
                        _context.EmployeeAttendance
                            .Where(x =>
                                x.AttendanceDate >= model.FromDate &&
                                x.AttendanceDate <= model.ToDate &&
                                !x.IsDeleted)
                            .OrderBy(x => x.AttendanceDate)
                            .ToList();
                }
                else
                {
                    model.AttendanceList =
                        _context.EmployeeAttendance
                            .Where(x =>
                                x.EmployeeId == model.EmployeeId &&
                                x.AttendanceDate >= model.FromDate &&
                                x.AttendanceDate <= model.ToDate &&
                                !x.IsDeleted)
                            .OrderBy(x => x.AttendanceDate)
                            .ToList();
                }

                // =====================================================
                // APPROVED LEAVES
                // =====================================================

                var approvedLeaveList =
                    _context.LeaveRequests
                        .Where(x =>
                            x.Status == "Approved" &&
                            x.FromDate <= model.ToDate &&
                            x.ToDate >= model.FromDate)
                        .ToList();

                // =====================================================
                // TOTAL DAYS
                // =====================================================

                model.TotalDays =
                    DateTime.DaysInMonth(
                        model.FromDate.Year,
                        model.FromDate.Month);

                // =====================================================
                // WEEKENDS
                // =====================================================

                CalculateWeekendInformation(
                    model.FromDate,
                    model.ToDate,
                    out int saturdayCount,
                    out int sundayCount);

                model.SaturdayCount = saturdayCount;
                model.SundayCount = sundayCount;

                model.HolidayCount =
                    saturdayCount + sundayCount;

                model.WorkingDays =
                    model.TotalDays -
                    model.HolidayCount;

                // =====================================================
                // SELECTED EMPLOYEE
                // =====================================================

                if (model.EmployeeId != 0)
                {
                    var employee =
                        _context.MasterEmployee
                            .FirstOrDefault(x =>
                                x.EmployeeId ==
                                    model.EmployeeId &&
                                !x.IsDeleted);

                    if (employee != null)
                    {
                        model.MonthlySalary =
                            employee.Salary;

                        model.PaidLeaveBalance =
                            employee.PaidLeaveBalance;

                        var employeeAttendance =
                            model.AttendanceList
                                .Where(x =>
                                    x.EmployeeId ==
                                    employee.EmployeeId)
                                .ToList();

                        var result =
                            CalculateEmployeeAttendance(
                                employee,
                                model.FromDate,
                                model.ToDate,
                                employeeAttendance,
                                approvedLeaveList);

                        CopyCalculationToViewModel(
                            model,
                            result);
                    }
                }

                // =====================================================
                // ALL EMPLOYEES
                // =====================================================

                else
                {
                    model.EmployeeSummaryList = new();

                    foreach (var employee in model.EmployeeList)
                    {
                        var employeeAttendance =
                            model.AttendanceList
                                .Where(x =>
                                    x.EmployeeId ==
                                    employee.EmployeeId)
                                .ToList();

                        var result =
                            CalculateEmployeeAttendance(
                                employee,
                                model.FromDate,
                                model.ToDate,
                                employeeAttendance,
                                approvedLeaveList);

                        model.EmployeeSummaryList.Add(result);
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("ViewAttendance");
            }
        }



        private EmployeeAttendanceSummaryViewModel CalculateEmployeeAttendance(MasterEmployee employee,DateOnly fromDate,DateOnly toDate, List<EmployeeAttendance> attendanceList,
         List<LeaveRequests> approvedLeaveList)
        {
            EmployeeAttendanceSummaryViewModel result = new EmployeeAttendanceSummaryViewModel();

            try
            {
                // =====================================================
                // EMPLOYEE INFORMATION
                // =====================================================

                result.EmployeeId =
                    employee.EmployeeId;

                result.EmployeeCode =
                    employee.EmployeeCode;

                result.FullName =
                    employee.FullName;

                result.MonthlySalary =
                    employee.Salary;


                // =====================================================
                // REMOVE DUPLICATE ATTENDANCE DATES
                // =====================================================

                var uniqueAttendance =
                    attendanceList
                        .Where(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            !x.IsDeleted)
                        .GroupBy(x => x.AttendanceDate)
                        .Select(g => g.First())
                        .OrderBy(x => x.AttendanceDate)
                        .ToList();


                // =====================================================
                // ATTENDANCE COUNTS
                // =====================================================

                foreach (var attendance in uniqueAttendance)
                {
                    switch (attendance.Status)
                    {
                        case "Present":
                            result.PresentDays++;
                            break;

                        case "PL":
                            result.PaidLeaveCount++;
                            break;

                        case "HD":
                            result.HalfDayCount++;
                            break;

                        case "H":
                            result.OfficialHolidayCount++;
                            break;

                        case "LWP":
                            result.LWPCount++;
                            break;

                        case "ULWP":
                            result.ULWPCount++;
                            break;

                        case "NCNS":
                            result.NCNSCount++;
                            break;

                        case "Abscond":
                            result.AbscondCount++;
                            break;

                        case "Terminated":
                            result.TerminatedCount++;
                            break;
                    }
                }


                // =====================================================
                // APPROVED LEAVE COUNTS
                // =====================================================

                for (
                    DateOnly date = fromDate;
                    date <= toDate;
                    date = date.AddDays(1))
                {
                    bool attendanceExists =
                        uniqueAttendance.Any(x =>
                            x.AttendanceDate == date);

                    if (attendanceExists)
                        continue;

                    var leave =
                        approvedLeaveList.FirstOrDefault(x =>
                            x.UserId == employee.EmployeeId &&
                            date >= x.FromDate &&
                            date <= x.ToDate);

                    if (leave == null)
                        continue;

                    switch (leave.LeaveTypeId)
                    {
                        case 1:
                        case 2:

                            result.PaidLeaveCount++;
                            break;

                        case 3:

                            result.WorkFromHomeCount++;
                            break;

                        case 4:

                            result.HalfDayCount++;
                            break;

                        case 5:

                            result.EmergencyLeaveCount++;
                            break;
                    }
                }


                // =====================================================
                // SANDWICH LEAVE
                //
                // Only Friday/Monday is considered the actual
                // Sandwich Leave event.
                //
                // Saturday/Sunday generated SL records are not
                // counted as separate events.
                // =====================================================

                var sandwichDates =
                    uniqueAttendance
                        .Where(x =>
                            x.Status == "SL" &&
                            (
                                x.AttendanceDate.DayOfWeek ==
                                    DayOfWeek.Friday
                                ||
                                x.AttendanceDate.DayOfWeek ==
                                    DayOfWeek.Monday
                            ))
                        .Select(x => x.AttendanceDate)
                        .Distinct()
                        .ToList();

                result.SandwichCount =
                    sandwichDates.Count;


                // =====================================================
                // WEEKEND INFORMATION
                // =====================================================

                CalculateWeekendInformation(
                    fromDate,
                    toDate,
                    out int saturdayCount,
                    out int sundayCount);

                result.HolidayCount =
                    saturdayCount + sundayCount;


                // =====================================================
                // ATTENDANCE DAYS
                // =====================================================

                result.AttendanceDays =
                      result.PresentDays
                    + result.PaidLeaveCount
                    + result.WorkFromHomeCount
                    + result.EmergencyLeaveCount
                    + result.OfficialHolidayCount
                    + result.HolidayCount
                    + (result.HalfDayCount * 0.5M);


                // =====================================================
                // TOTAL DAYS IN MONTH
                // =====================================================

                int totalDays =
                    DateTime.DaysInMonth(
                        fromDate.Year,
                        fromDate.Month);


                // =====================================================
                // ATTENDANCE PERCENTAGE
                // =====================================================

                if (totalDays > 0)
                {
                    result.AttendancePercentage =
                        Math.Round(
                            (
                                (double)result.AttendanceDays /
                                totalDays
                            ) * 100,
                            2);
                }


                // =====================================================
                // SALARY INFORMATION
                // =====================================================

                decimal monthlySalary =
                    employee.Salary;

                decimal perDaySalary =
                    totalDays > 0
                        ? monthlySalary / totalDays
                        : 0M;

                result.PerDaySalary =
                    Math.Round(
                        perDaySalary,
                        2);


                // =====================================================
                // FIND FIRST ABSCOND DATE
                // =====================================================

                DateOnly? abscondDate =
                    uniqueAttendance
                        .Where(x =>
                            x.Status == "Abscond")
                        .OrderBy(x => x.AttendanceDate)
                        .Select(x =>
                            (DateOnly?)x.AttendanceDate)
                        .FirstOrDefault();


                // =====================================================
                // FIND FIRST TERMINATED DATE
                // =====================================================

                DateOnly? terminatedDate =
                    uniqueAttendance
                        .Where(x =>
                            x.Status == "Terminated")
                        .OrderBy(x => x.AttendanceDate)
                        .Select(x =>
                            (DateOnly?)x.AttendanceDate)
                        .FirstOrDefault();


                // =====================================================
                // DETERMINE SALARY END DATE
                //
                // NORMAL:
                // Full month salary.
                //
                // ABSCOND:
                // Salary until previous day.
                //
                // TERMINATED:
                // Salary including termination date.
                //
                // If both exist, the earlier applicable cutoff
                // is used.
                // =====================================================

                DateOnly salaryEndDate =
                    toDate;


                // =====================================================
                // ABSCOND SALARY CUTOFF
                //
                // Example:
                // Abscond = 15-Sep
                // Salary = 01-Sep to 14-Sep
                // =====================================================

                if (abscondDate.HasValue)
                {
                    DateOnly abscondSalaryEnd =
                        abscondDate.Value.AddDays(-1);

                    if (abscondSalaryEnd < salaryEndDate)
                    {
                        salaryEndDate =
                            abscondSalaryEnd;
                    }
                }


                // =====================================================
                // TERMINATED SALARY CUTOFF
                //
                // Example:
                // Terminated = 15-Sep
                // Salary = 01-Sep to 15-Sep
                // =====================================================

                if (terminatedDate.HasValue)
                {
                    DateOnly terminatedSalaryEnd =
                        terminatedDate.Value;

                    if (terminatedSalaryEnd < salaryEndDate)
                    {
                        salaryEndDate =
                            terminatedSalaryEnd;
                    }
                }


                // =====================================================
                // SALARY DAYS
                // =====================================================

                decimal salaryDays = 0M;

                if (salaryEndDate >= fromDate)
                {
                    salaryDays =
                        salaryEndDate.Day;
                }


                // =====================================================
                // SALARY BEFORE DEDUCTIONS
                //
                // CASE 1:
                // NORMAL EMPLOYEE
                // No Abscond and no Terminated.
                //
                // Result = FULL MONTHLY SALARY.
                //
                // CASE 2:
                // ABSCOND OR TERMINATED.
                //
                // Result = salary according to cutoff date.
                // =====================================================

                decimal salaryBeforeDeductions;

                if (!abscondDate.HasValue &&
                    !terminatedDate.HasValue)
                {
                    // -------------------------------------------------
                    // NORMAL EMPLOYEE
                    // -------------------------------------------------

                    salaryBeforeDeductions =
                        monthlySalary;
                }
                else
                {
                    // -------------------------------------------------
                    // ABSCOND / TERMINATED EMPLOYEE
                    // -------------------------------------------------

                    salaryBeforeDeductions =
                        perDaySalary * salaryDays;
                }


                // =====================================================
                // ATTENDANCE WITHIN SALARY PERIOD
                // =====================================================

                var salaryAttendance =
                    uniqueAttendance
                        .Where(x =>
                            x.AttendanceDate >= fromDate &&
                            x.AttendanceDate <= salaryEndDate)
                        .ToList();


                // =====================================================
                // HALF DAY COUNT
                // =====================================================

                int halfDayCount =
                    salaryAttendance.Count(x =>
                        x.Status == "HD");


                // =====================================================
                // LWP COUNT
                // =====================================================

                int lwpCount =
                    salaryAttendance.Count(x =>
                        x.Status == "LWP");


                // =====================================================
                // ULWP COUNT
                // =====================================================

                int ulwpCount =
                    salaryAttendance.Count(x =>
                        x.Status == "ULWP");


                // =====================================================
                // NCNS COUNT
                // =====================================================

                int ncnsCount =
                    salaryAttendance.Count(x =>
                        x.Status == "NCNS");


                // =====================================================
                // SANDWICH EVENTS
                // =====================================================

                int sandwichCount =
                    salaryAttendance
                        .Where(x =>
                            x.Status == "SL" &&
                            (
                                x.AttendanceDate.DayOfWeek ==
                                    DayOfWeek.Friday
                                ||
                                x.AttendanceDate.DayOfWeek ==
                                    DayOfWeek.Monday
                            ))
                        .Select(x =>
                            x.AttendanceDate)
                        .Distinct()
                        .Count();


                // =====================================================
                // HALF DAY DEDUCTION
                // =====================================================

                result.HalfDayDeduction =
                    Math.Round(
                        halfDayCount *
                        perDaySalary *
                        0.5M,
                        2);


                // =====================================================
                // LWP DEDUCTION
                // =====================================================

                result.LWPDeduction =
                    Math.Round(
                        lwpCount *
                        perDaySalary,
                        2);


                // =====================================================
                // ULWP DEDUCTION
                // =====================================================

                result.ULWPDeduction =
                    Math.Round(
                        ulwpCount *
                        perDaySalary *
                        2M,
                        2);


                // =====================================================
                // SANDWICH LEAVE DEDUCTION
                //
                // One Sandwich Leave event = 3 days deduction.
                // =====================================================

                result.SandwichDeduction =
                    Math.Round(
                        sandwichCount *
                        perDaySalary *
                        3M,
                        2);


                // =====================================================
                // NCNS DEDUCTION
                //
                // One NCNS = 3 days deduction.
                // =====================================================

                result.NCNSDeduction =
                    Math.Round(
                        ncnsCount *
                        perDaySalary *
                        3M,
                        2);


                // =====================================================
                // ABSCOND / TERMINATED DEDUCTION
                //
                // Their salary effect is already handled through
                // salaryBeforeDeductions.
                // =====================================================

                result.AbscondDeduction =
                    0M;

                result.TerminatedDeduction =
                    0M;


                // =====================================================
                // TOTAL SALARY DEDUCTION
                // =====================================================

                result.SalaryDeduction =
                    Math.Round(
                          result.HalfDayDeduction
                        + result.LWPDeduction
                        + result.ULWPDeduction
                        + result.SandwichDeduction
                        + result.NCNSDeduction,
                        2);


                // =====================================================
                // NET SALARY
                // =====================================================

                result.NetSalary =
                    salaryBeforeDeductions -
                    result.SalaryDeduction;


                // =====================================================
                // PREVENT NEGATIVE SALARY
                // =====================================================

                if (result.NetSalary < 0)
                {
                    result.NetSalary =
                        0M;
                }


                // =====================================================
                // FINAL ROUNDING
                // =====================================================

                result.NetSalary =
                    Math.Round(
                        result.NetSalary,
                        2);


                return result;
            }
            catch
            {
                throw;
            }
        }

        private void CopyCalculationToViewModel(
           AttendanceReportViewModel model,
           EmployeeAttendanceSummaryViewModel result)
        {
            model.PresentDays =
                result.PresentDays;

            model.PaidLeaveCount =
                result.PaidLeaveCount;

            model.WorkFromHomeCount =
                result.WorkFromHomeCount;

            model.HalfDayCount =
                result.HalfDayCount;

            model.EmergencyLeaveCount =
                result.EmergencyLeaveCount;

            model.LWPCount =
                result.LWPCount;

            model.ULWPCount =
                result.ULWPCount;

            model.SandwichCount =
                result.SandwichCount;

            model.NCNSCount =
                result.NCNSCount;

            model.AbscondCount =
                result.AbscondCount;

            model.TerminatedCount =
                result.TerminatedCount;

            model.OfficialHolidayCount =
                result.OfficialHolidayCount;

            model.HolidayCount =
                result.HolidayCount;

            model.AttendanceDays =
                result.AttendanceDays;

            model.AttendancePercentage =
                result.AttendancePercentage;

            model.MonthlySalary =
                result.MonthlySalary;

            model.PerDaySalary =
                result.PerDaySalary;

            model.HalfDayDeduction =
                result.HalfDayDeduction;

            model.LWPDeduction =
                result.LWPDeduction;

            model.ULWPDeduction =
                result.ULWPDeduction;

            model.SandwichDeduction =
                result.SandwichDeduction;

            model.NCNSDeduction =
                result.NCNSDeduction;

            model.AbscondDeduction =
                result.AbscondDeduction;

            model.TerminatedDeduction =
                result.TerminatedDeduction;

            model.SalaryDeduction =
                result.SalaryDeduction;

            model.NetSalary =
                result.NetSalary;
        }



        private void CalculateWeekendInformation(DateOnly fromDate,DateOnly toDate,out int saturdayCount, out int sundayCount)
        {
            saturdayCount = 0;
            sundayCount = 0;

            for (
                DateOnly date = fromDate;
                date <= toDate;
                date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    sundayCount++;
                }

                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    int saturdayNumber = 0;

                    for (
                        DateOnly d =
                            new DateOnly(
                                date.Year,
                                date.Month,
                                1);
                        d <= date;
                        d = d.AddDays(1))
                    {
                        if (d.DayOfWeek ==
                            DayOfWeek.Saturday)
                        {
                            saturdayNumber++;
                        }
                    }

                    if (saturdayNumber <= 4)
                    {
                        saturdayCount++;
                    }
                }
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAttendance(int attendanceId)
        {
            try
            {
                // ==========================================
                // FIND ATTENDANCE RECORD
                // ==========================================
                var attendance = _context.EmployeeAttendance
                    .FirstOrDefault(x => x.AttendanceId == attendanceId);

                // ==========================================
                // RECORD NOT FOUND
                // ==========================================
                if (attendance == null)
                {
                    TempData["Error"] = "Attendance record not found.";

                    return RedirectToAction("ViewAttendance");
                }

                // ==========================================
                // DELETE RECORD
                // ==========================================
                _context.EmployeeAttendance.Remove(attendance);

                _context.SaveChanges();

                // ==========================================
                // SUCCESS MESSAGE
                // ==========================================
                TempData["Success"] =
                    "Attendance entry deleted successfully.";
            }
            catch (Exception ex)
            {

                TempData["Error"] =
                    "An error occurred while deleting the attendance entry.";
            }

            return RedirectToAction("ViewAttendance");
        }

        private void AddAttendanceRecord(int employeeId, DateOnly attendanceDate,string status,string? remarks = null)
        {
            bool exists = _context.EmployeeAttendance.Any(x =>
                x.EmployeeId == employeeId &&
                x.AttendanceDate == attendanceDate &&
                !x.IsDeleted);

            if (exists)
            {
                return;
            }

            _context.EmployeeAttendance.Add(
                new EmployeeAttendance
                {
                    EmployeeId = employeeId,
                    AttendanceDate = attendanceDate,
                    Status = status,
                    Remarks = remarks,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    IsDeleted = false
                });
        }

        private void ApplySandwichLeave(int employeeId,DateOnly leaveDate,string? remarks = null)
        {
            List<DateOnly> dates = new List<DateOnly>();

            // ========================================================
            // FRIDAY
            // FRIDAY + SATURDAY + SUNDAY
            // ========================================================

            if (leaveDate.DayOfWeek == DayOfWeek.Friday)
            {
                dates.Add(leaveDate);
                dates.Add(leaveDate.AddDays(1));
                dates.Add(leaveDate.AddDays(2));
            }

            // ========================================================
            // MONDAY
            // SATURDAY + SUNDAY + MONDAY
            // ========================================================

            else if (leaveDate.DayOfWeek == DayOfWeek.Monday)
            {
                dates.Add(leaveDate.AddDays(-2));
                dates.Add(leaveDate.AddDays(-1));
                dates.Add(leaveDate);
            }

            else
            {
                return;
            }

            // ========================================================
            // ADD ONLY IF NOT EXISTS
            // ========================================================

            foreach (DateOnly day in dates.Distinct())
            {
                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate == day &&
                    !x.IsDeleted);

                if (exists)
                {
                    // Do not create duplicate.
                    continue;
                }

                _context.EmployeeAttendance.Add(
                    new EmployeeAttendance
                    {
                        EmployeeId = employeeId,
                        AttendanceDate = day,
                        Status = "SL",
                        Remarks = remarks,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        IsDeleted = false
                    });
            }

            // IMPORTANT:
            // Do NOT call SaveChanges here.
            //
            // SaveAttendance() will save everything once.
        }

        private void ApplyRemainingMonthStatus( int employeeId,DateOnly startDate, string status)
        {
            DateOnly lastDay = new DateOnly(
                startDate.Year,
                startDate.Month,
                DateTime.DaysInMonth(
                    startDate.Year,
                    startDate.Month));

            // ========================================================
            // START FROM NEXT DAY
            //
            // The selected Abscond/Terminated date was already added.
            // ========================================================

            DateOnly day = startDate.AddDays(1);

            while (day <= lastDay)
            {
                bool exists = _context.EmployeeAttendance.Any(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate == day &&
                    !x.IsDeleted);

                if (!exists)
                {
                    _context.EmployeeAttendance.Add(
                        new EmployeeAttendance
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

                day = day.AddDays(1);
            }

            // Do NOT SaveChanges here.
        }

        private bool IsCompanyWeekendHoliday(DateOnly date)
        {
            // Sunday = Holiday
            if (date.DayOfWeek ==
                DayOfWeek.Sunday)
            {
                return true;
            }

            // First four Saturdays = Holiday
            if (date.DayOfWeek ==
                DayOfWeek.Saturday)
            {
                int saturdayNumber = 0;

                for (
                    DateOnly d =
                        new DateOnly(
                            date.Year,
                            date.Month,
                            1);
                    d <= date;
                    d = d.AddDays(1))
                {
                    if (d.DayOfWeek ==
                        DayOfWeek.Saturday)
                    {
                        saturdayNumber++;
                    }
                }

                return saturdayNumber <= 4;
            }

            return false;
        }

        [HttpGet]
        public async Task<IActionResult> EditAttendance(int attendanceId)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var attendance =
                    await _context.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.AttendanceId == attendanceId &&
                            !x.IsDeleted);

                if (attendance == null)
                {
                    TempData["Error"] =
                        "Attendance record not found.";

                    return RedirectToAction("ViewAttendance");
                }

                var employee =
                    await _context.MasterEmployee
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == attendance.EmployeeId &&
                            !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] =
                        "Employee not found.";

                    return RedirectToAction("ViewAttendance");
                }

                HRDashboardViewModel model =
                    new HRDashboardViewModel();

                model.MasterEmployee = employee;
                model.EmployeeAttendance = attendance;

                model.EmployeeList =
                    await _context.MasterEmployee
                        .Where(x => !x.IsDeleted)
                        .OrderBy(x => x.FullName)
                        .ToListAsync();

                model.IsAttendanceEdit = true;

                return View("Dashboard", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("ViewAttendance");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(HRDashboardViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (model.EmployeeAttendance == null)
                {
                    TempData["Error"] =
                        "Attendance information is missing.";

                    return RedirectToAction("ViewAttendance");
                }

                var attendanceModel =
                    model.EmployeeAttendance;

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

                // =====================================================
                // VALID STATUS
                // =====================================================

                string[] validStatuses =
                {
                    "Present",
                    "PL",
                    "HD",
                    "H",
                    "LWP",
                    "ULWP",
                    "SL",
                    "NCNS",
                    "Abscond",
                    "Terminated"
                };

                if (!validStatuses.Contains(
                        attendanceModel.Status))
                {
                    TempData["Error"] =
                        "Invalid attendance status.";

                    return RedirectToAction(
                        "EditAttendance",
                        new
                        {
                            attendanceId =
                                attendanceModel.AttendanceId
                        });
                }

                // =====================================================
                // DUPLICATE CHECK
                // =====================================================

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

                // =====================================================
                // SANDWICH VALIDATION
                // =====================================================

                if (attendanceModel.Status == "SL")
                {
                    if (attendanceModel.AttendanceDate.DayOfWeek !=
                            DayOfWeek.Friday &&
                        attendanceModel.AttendanceDate.DayOfWeek !=
                            DayOfWeek.Monday)
                    {
                        TempData["Error"] =
                            "Sandwich Leave can only be applied on Friday or Monday.";

                        return RedirectToAction(
                            "EditAttendance",
                            new
                            {
                                attendanceId =
                                    attendanceModel.AttendanceId
                            });
                    }
                }

                // =====================================================
                // UPDATE EXISTING RECORD
                // =====================================================

                attendance.EmployeeId =
                    attendanceModel.EmployeeId;

                attendance.AttendanceDate =
                    attendanceModel.AttendanceDate;

                attendance.Status =
                    attendanceModel.Status;

                attendance.Remarks =
                    attendanceModel.Remarks;

                attendance.UpdatedDate =
                    DateTime.Now;

                attendance.ModifiedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Attendance updated successfully.";

                return RedirectToAction("ViewAttendance");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

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

        [HttpPost]
        public IActionResult ExportAttendanceExcel(AttendanceReportViewModel model)
        {
            try
            {
                // =====================================================
                // ROLE VALIDATION
                // =====================================================

                int? roleId =
                    HttpContext.Session.GetInt32("RoleId");

                if (roleId != 5 && roleId != 3)
                {
                    return RedirectToAction(
                        "Login",
                        "Account");
                }

                // =====================================================
                // DATE VALIDATION
                // =====================================================

                if (model.FromDate > model.ToDate)
                {
                    TempData["Error"] =
                        "From Date cannot be greater than To Date.";

                    return RedirectToAction(
                        "ViewAttendance");
                }

                if (model.FromDate.Year !=
                        model.ToDate.Year ||
                    model.FromDate.Month !=
                        model.ToDate.Month)
                {
                    TempData["Error"] =
                        "Please select From Date and To Date within the same month.";

                    return RedirectToAction(
                        "ViewAttendance");
                }

                // =====================================================
                // EMPLOYEES
                // =====================================================

                List<MasterEmployee> employees;

                if (model.EmployeeId == 0)
                {
                    employees =
                        _context.MasterEmployee
                            .Where(x => !x.IsDeleted)
                            .OrderBy(x => x.FullName)
                            .ToList();
                }
                else
                {
                    employees =
                        _context.MasterEmployee
                            .Where(x =>
                                x.EmployeeId ==
                                    model.EmployeeId &&
                                !x.IsDeleted)
                            .ToList();
                }

                if (!employees.Any())
                {
                    TempData["Error"] =
                        "No employee found.";

                    return RedirectToAction(
                        "ViewAttendance");
                }

                // =====================================================
                // ATTENDANCE
                // =====================================================

                var attendanceList =
                    _context.EmployeeAttendance
                        .Where(x =>
                            x.AttendanceDate >=
                                model.FromDate &&
                            x.AttendanceDate <=
                                model.ToDate &&
                            !x.IsDeleted)
                        .ToList();

                // =====================================================
                // APPROVED LEAVES
                // =====================================================

                var approvedLeaveList =
                    _context.LeaveRequests
                        .Where(x =>
                            x.Status == "Approved" &&
                            x.FromDate <=
                                model.ToDate &&
                            x.ToDate >=
                                model.FromDate)
                        .ToList();

                // =====================================================
                // WEEKEND COUNTS
                // =====================================================

                CalculateWeekendInformation(
                    model.FromDate,
                    model.ToDate,
                    out int saturdayCount,
                    out int sundayCount);

                int holidayCount =
                    saturdayCount +
                    sundayCount;

                int totalDays =
                    DateTime.DaysInMonth(
                        model.FromDate.Year,
                        model.FromDate.Month);

                // =====================================================
                // EXCEL WORKBOOK
                // =====================================================

                using var workbook =
                    new XLWorkbook();

                var ws =
                    workbook.Worksheets.Add(
                        "Attendance Report");

                // =====================================================
                // HEADERS
                // =====================================================

                ws.Cell(1, 1).Value =
                    "Employee Code";

                ws.Cell(1, 2).Value =
                    "Employee Name";

                int dateStartColumn = 3;

                int currentColumn =
                    dateStartColumn;

                for (
                    DateOnly date = model.FromDate;
                    date <= model.ToDate;
                    date = date.AddDays(1))
                {
                    ws.Cell(
                        1,
                        currentColumn).Value =
                        date.ToString(
                            "dd-MMM");

                    currentColumn++;
                }

                // =====================================================
                // SUMMARY HEADERS
                // =====================================================

                int summaryStartColumn =
                    currentColumn;

                string[] summaryHeaders =
                {
            "Calendar Days",
            "Present",
            "PL",
            "WFH",
            "EL",
            "Holiday",
            "Official H",
            "HD",
            "LWP",
            "ULWP",
            "SL",
            "NCNS",
            "Abscond",
            "Terminated",
            "Attendance %",
            "Monthly Salary",
            "Per Day Salary",
            "HD Deduction",
            "LWP Deduction",
            "ULWP Deduction",
            "SL Deduction",
            "NCNS Deduction",
            "Salary Deduction",
            "Net Salary"
        };

                for (
                    int i = 0;
                    i < summaryHeaders.Length;
                    i++)
                {
                    ws.Cell(
                        1,
                        summaryStartColumn + i)
                        .Value =
                        summaryHeaders[i];
                }

                // =====================================================
                // HEADER STYLE
                // =====================================================

                var headerRange =
                    ws.Range(
                        1,
                        1,
                        1,
                        summaryStartColumn +
                        summaryHeaders.Length -
                        1);

                headerRange.Style.Font.Bold =
                    true;

                headerRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                headerRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                headerRange.Style.Fill.PatternType =
                    XLFillPatternValues.Solid;

                headerRange.Style.Fill.BackgroundColor =
                    XLColor.DarkBlue;

                headerRange.Style.Font.FontColor =
                    XLColor.White;

                // =====================================================
                // DATA ROW
                // =====================================================

                int row = 2;

                foreach (var employee in employees)
                {
                    ws.Cell(row, 1).Value =
                        employee.EmployeeCode;

                    ws.Cell(row, 2).Value =
                        employee.FullName;

                    var employeeAttendance =
                        attendanceList
                            .Where(x =>
                                x.EmployeeId ==
                                employee.EmployeeId)
                            .ToList();

                    // =================================================
                    // CALCULATE USING SAME METHOD AS UI
                    // =================================================

                    var result =
                        CalculateEmployeeAttendance(
                            employee,
                            model.FromDate,
                            model.ToDate,
                            employeeAttendance,
                            approvedLeaveList);

                    // =================================================
                    // DATE COLUMNS
                    // =================================================

                    currentColumn =
                        dateStartColumn;

                    for (
                        DateOnly date = model.FromDate;
                        date <= model.ToDate;
                        date = date.AddDays(1))
                    {
                        var attendance =
                            employeeAttendance
                                .Where(x =>
                                    x.AttendanceDate ==
                                        date &&
                                    !x.IsDeleted)
                                .OrderBy(x =>
                                    x.AttendanceId)
                                .FirstOrDefault();

                        string displayStatus =
                            string.Empty;

                        // =============================================
                        // ATTENDANCE STATUS
                        // =============================================

                        if (attendance != null)
                        {
                            switch (attendance.Status)
                            {
                                case "Present":
                                    displayStatus = "P";
                                    break;

                                case "PL":
                                    displayStatus = "PL";
                                    break;

                                case "HD":
                                    displayStatus = "HD";
                                    break;

                                case "H":
                                    displayStatus = "H";
                                    break;

                                case "LWP":
                                    displayStatus = "LWP";
                                    break;

                                case "ULWP":
                                    displayStatus = "ULWP";
                                    break;

                                case "SL":
                                    displayStatus = "SL";
                                    break;

                                case "NCNS":
                                    displayStatus = "NCNS";
                                    break;

                                case "Abscond":
                                    displayStatus = "ABS";
                                    break;

                                case "Terminated":
                                    displayStatus = "TERM";
                                    break;
                            }
                        }
                        else
                        {
                            // =========================================
                            // WEEKEND / COMPANY HOLIDAY
                            // =========================================

                            bool isHoliday =
                                IsCompanyWeekendHoliday(
                                    date);

                            if (isHoliday)
                            {
                                displayStatus = "H";
                            }
                            else
                            {
                                // =====================================
                                // APPROVED LEAVE
                                // =====================================

                                var leave =
                                    approvedLeaveList
                                        .FirstOrDefault(x =>
                                            x.UserId ==
                                                employee.EmployeeId &&
                                            date >=
                                                x.FromDate &&
                                            date <=
                                                x.ToDate);

                                if (leave != null)
                                {
                                    switch (
                                        leave.LeaveTypeId)
                                    {
                                        case 1:
                                        case 2:
                                            displayStatus = "PL";
                                            break;

                                        case 3:
                                            displayStatus = "WFH";
                                            break;

                                        case 4:
                                            displayStatus = "HD";
                                            break;

                                        case 5:
                                            displayStatus = "EL";
                                            break;
                                    }
                                }
                            }
                        }

                        // =============================================
                        // WRITE STATUS
                        // =============================================

                        var statusCell =
                            ws.Cell(
                                row,
                                currentColumn);

                        statusCell.Value =
                            displayStatus;

                        statusCell.Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                        statusCell.Style.Alignment.Vertical =
                            XLAlignmentVerticalValues.Center;

                        statusCell.Style.Font.Bold =
                            true;

                        // =============================================
                        // STATUS COLORS
                        // =============================================

                        switch (displayStatus)
                        {
                            // -----------------------------------------
                            // PRESENT
                            // -----------------------------------------
                            case "P":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightGreen;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkGreen;

                                break;

                            // -----------------------------------------
                            // PAID LEAVE
                            // -----------------------------------------
                            case "PL":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightGreen;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkGreen;

                                break;

                            // -----------------------------------------
                            // WORK FROM HOME
                            // -----------------------------------------
                            case "WFH":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightBlue;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkBlue;

                                break;

                            // -----------------------------------------
                            // HALF DAY
                            // -----------------------------------------
                            case "HD":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightYellow;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkOrange;

                                break;

                            // -----------------------------------------
                            // HOLIDAY
                            // -----------------------------------------
                            case "H":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightGray;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkGray;

                                break;

                            // -----------------------------------------
                            // EMERGENCY LEAVE
                            // -----------------------------------------
                            case "EL":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightPink;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkRed;

                                break;

                            // -----------------------------------------
                            // LWP
                            // -----------------------------------------
                            case "LWP":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.LightCoral;

                                statusCell.Style.Font.FontColor =
                                    XLColor.DarkRed;

                                break;

                            // -----------------------------------------
                            // ULWP
                            // -----------------------------------------
                            case "ULWP":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.Red;

                                statusCell.Style.Font.FontColor =
                                    XLColor.White;

                                break;

                            // -----------------------------------------
                            // SANDWICH LEAVE
                            // -----------------------------------------
                            case "SL":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.MediumPurple;

                                statusCell.Style.Font.FontColor =
                                    XLColor.White;

                                break;

                            // -----------------------------------------
                            // NCNS
                            // -----------------------------------------
                            case "NCNS":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.DarkRed;

                                statusCell.Style.Font.FontColor =
                                    XLColor.White;

                                break;

                            // -----------------------------------------
                            // ABSCOND
                            // -----------------------------------------
                            case "ABS":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.Orange;

                                statusCell.Style.Font.FontColor =
                                    XLColor.White;

                                break;

                            // -----------------------------------------
                            // TERMINATED
                            // -----------------------------------------
                            case "TERM":

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.Black;

                                statusCell.Style.Font.FontColor =
                                    XLColor.White;

                                break;

                            // -----------------------------------------
                            // EMPTY
                            // -----------------------------------------
                            default:

                                statusCell.Style.Fill.PatternType =
                                    XLFillPatternValues.Solid;

                                statusCell.Style.Fill.BackgroundColor =
                                    XLColor.White;

                                break;
                        }

                        currentColumn++;
                    }

                    // =================================================
                    // SUMMARY
                    // =================================================

                    int col =
                        summaryStartColumn;

                    ws.Cell(row, col++).Value =
                        totalDays;

                    ws.Cell(row, col++).Value =
                        result.PresentDays;

                    ws.Cell(row, col++).Value =
                        result.PaidLeaveCount;

                    ws.Cell(row, col++).Value =
                        result.WorkFromHomeCount;

                    ws.Cell(row, col++).Value =
                        result.EmergencyLeaveCount;

                    ws.Cell(row, col++).Value =
                        result.HolidayCount;

                    ws.Cell(row, col++).Value =
                        result.OfficialHolidayCount;

                    ws.Cell(row, col++).Value =
                        result.HalfDayCount;

                    ws.Cell(row, col++).Value =
                        result.LWPCount;

                    ws.Cell(row, col++).Value =
                        result.ULWPCount;

                    ws.Cell(row, col++).Value =
                        result.SandwichCount;

                    ws.Cell(row, col++).Value =
                        result.NCNSCount;

                    ws.Cell(row, col++).Value =
                        result.AbscondCount;

                    ws.Cell(row, col++).Value =
                        result.TerminatedCount;

                    ws.Cell(row, col++).Value =
                        result.AttendancePercentage / 100;

                    ws.Cell(row, col++).Value =
                        result.MonthlySalary;

                    ws.Cell(row, col++).Value =
                        result.PerDaySalary;

                    ws.Cell(row, col++).Value =
                        result.HalfDayDeduction;

                    ws.Cell(row, col++).Value =
                        result.LWPDeduction;

                    ws.Cell(row, col++).Value =
                        result.ULWPDeduction;

                    ws.Cell(row, col++).Value =
                        result.SandwichDeduction;

                    ws.Cell(row, col++).Value =
                        result.NCNSDeduction;

                    ws.Cell(row, col++).Value =
                        result.SalaryDeduction;

                    ws.Cell(row, col++).Value =
                        result.NetSalary;

                    row++;
                }

                // =====================================================
                // SUMMARY HEADER COLORS
                // =====================================================

                // Attendance related summary columns
                ws.Range(
                    1,
                    summaryStartColumn,
                    1,
                    summaryStartColumn +
                        summaryHeaders.Length - 1)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                // =====================================================
                // NUMBER FORMATTING
                // =====================================================

                ws.Columns().AdjustToContents();

                // =====================================================
                // ATTENDANCE PERCENTAGE
                // =====================================================

                int percentageColumn =
                    summaryStartColumn + 14;

                ws.Column(
                    percentageColumn)
                    .Style.NumberFormat.Format =
                    "0.00%";

                // =====================================================
                // SALARY COLUMNS
                // =====================================================

                int monthlySalaryColumn =
                    summaryStartColumn + 15;

                ws.Range(
                    2,
                    monthlySalaryColumn,
                    row - 1,
                    summaryStartColumn +
                    summaryHeaders.Length -
                    1)
                    .Style.NumberFormat.Format =
                    "#,##0.00";

                // =====================================================
                // BORDERS
                // =====================================================

                var usedRange =
                    ws.RangeUsed();

                if (usedRange != null)
                {
                    usedRange.Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;

                    usedRange.Style.Border.InsideBorder =
                        XLBorderStyleValues.Thin;
                }

                // =====================================================
                // FREEZE HEADER
                // =====================================================

                ws.SheetView.FreezeRows(1);

                // =====================================================
                // DOWNLOAD
                // =====================================================

                using var stream =
                    new MemoryStream();

                workbook.SaveAs(stream);

                stream.Position = 0;

                string fileName =
                    $"Attendance_Report_{model.FromDate:yyyy_MM}.xlsx";

                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;

                return RedirectToAction(
                    "ViewAttendance");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportEmployeesToExcel()
        {
            try
            {
                // ==========================================
                // GET ACTIVE EMPLOYEES
                // ==========================================

                var employees = await _context.MasterEmployee
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();


                // ==========================================
                // EPPLUS LICENSE
                // ==========================================

                ExcelPackage.License.SetNonCommercialPersonal("Admin");


                // ==========================================
                // CREATE EXCEL PACKAGE
                // ==========================================

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Employees");


                    // ==========================================
                    // EXCEL HEADERS
                    // SAME SEQUENCE AS EMPLOYEE MASTER UI
                    // ==========================================

                    ws.Cells[1, 1].Value = "Employee Code";
                    ws.Cells[1, 2].Value = "Full Name";
                    ws.Cells[1, 3].Value = "Email";
                    ws.Cells[1, 4].Value = "Mobile Number";
                    ws.Cells[1, 5].Value = "Emergency Contact Name";
                    ws.Cells[1, 6].Value = "Emergency Contact";
                    ws.Cells[1, 7].Value = "Department";
                    ws.Cells[1, 8].Value = "Designation";
                    ws.Cells[1, 9].Value = "Gender";
                    ws.Cells[1, 10].Value = "Date Of Birth";
                    ws.Cells[1, 11].Value = "Joining Date";
                    ws.Cells[1, 12].Value = "Salary";
                    ws.Cells[1, 13].Value = "Status";
                    ws.Cells[1, 14].Value = "Blood Group";
                    ws.Cells[1, 15].Value = "Marital Status";
                    ws.Cells[1, 16].Value = "Address";
                    ws.Cells[1, 17].Value = "Pincode";
                    ws.Cells[1, 18].Value = "Employment Type";
                    ws.Cells[1, 19].Value = "Reporting Manager";
                    ws.Cells[1, 20].Value = "Shift";
                    ws.Cells[1, 21].Value = "Aadhaar Number";
                    ws.Cells[1, 22].Value = "PAN Number";
                    ws.Cells[1, 23].Value = "Bank Name";
                    ws.Cells[1, 24].Value = "Account Number";
                    ws.Cells[1, 25].Value = "IFSC Code";
                    ws.Cells[1, 26].Value = "Created Date";
                    ws.Cells[1, 27].Value = "Updated Date";


                    // ==========================================
                    // HEADER STYLE
                    // ==========================================

                    using (var range = ws.Cells[1, 1, 1, 27])
                    {
                        range.Style.Font.Bold = true;

                        range.Style.Fill.PatternType =
                            ExcelFillStyle.Solid;

                        range.Style.Fill.BackgroundColor.SetColor(
                            System.Drawing.Color.DarkBlue);

                        range.Style.Font.Color.SetColor(
                            System.Drawing.Color.White);
                    }


                    // ==========================================
                    // EMPLOYEE DATA
                    // ==========================================

                    int row = 2;

                    foreach (var emp in employees)
                    {
                        // ==========================================
                        // 1. EMPLOYEE CODE
                        // ==========================================

                        ws.Cells[row, 1].Value =
                            emp.EmployeeCode;


                        // ==========================================
                        // 2. FULL NAME
                        // ==========================================

                        ws.Cells[row, 2].Value =
                            emp.FullName;


                        // ==========================================
                        // 3. EMAIL
                        // ==========================================

                        ws.Cells[row, 3].Value =
                            emp.Email;


                        // ==========================================
                        // 4. MOBILE NUMBER
                        // ==========================================

                        ws.Cells[row, 4].Value =
                            emp.MobileNumber;


                        // ==========================================
                        // 5. EMERGENCY CONTACT NAME
                        // ==========================================

                        ws.Cells[row, 5].Value =
                            emp.EmergencyContactName;


                        // ==========================================
                        // 6. EMERGENCY CONTACT
                        // ==========================================

                        ws.Cells[row, 6].Value =
                            emp.EmergencyContact;


                        // ==========================================
                        // 7. DEPARTMENT
                        // ==========================================

                        ws.Cells[row, 7].Value =
                            emp.Department;


                        // ==========================================
                        // 8. DESIGNATION
                        // ==========================================

                        ws.Cells[row, 8].Value =
                            emp.Designation;


                        // ==========================================
                        // 9. GENDER
                        // ==========================================

                        ws.Cells[row, 9].Value =
                            emp.Gender;


                        // ==========================================
                        // 10. DATE OF BIRTH
                        // ==========================================

                        ws.Cells[row, 10].Value =
                            emp.DateOfBirth.ToString("dd-MM-yyyy");


                        // ==========================================
                        // 11. JOINING DATE
                        // ==========================================

                        ws.Cells[row, 11].Value =
                            emp.JoiningDate.ToString("dd-MM-yyyy");


                        // ==========================================
                        // 12. SALARY
                        // ==========================================

                        ws.Cells[row, 12].Value =
                            emp.Salary;


                        // ==========================================
                        // 13. STATUS
                        // ==========================================

                        ws.Cells[row, 13].Value =
                            emp.EmployeeStatus;


                        // ==========================================
                        // 14. BLOOD GROUP
                        // ==========================================

                        ws.Cells[row, 14].Value =
                            emp.BloodGroup;


                        // ==========================================
                        // 15. MARITAL STATUS
                        // ==========================================

                        ws.Cells[row, 15].Value =
                            emp.MaritalStatus;


                        // ==========================================
                        // 16. ADDRESS
                        // ==========================================

                        ws.Cells[row, 16].Value =
                            emp.AddressLine1;


                        // ==========================================
                        // 17. PINCODE
                        // ==========================================

                        ws.Cells[row, 17].Value =
                            emp.Pincode;


                        // ==========================================
                        // 18. EMPLOYMENT TYPE
                        // ==========================================

                        ws.Cells[row, 18].Value =
                            emp.EmploymentType;


                        // ==========================================
                        // 19. REPORTING MANAGER
                        // ==========================================

                        ws.Cells[row, 19].Value =
                            emp.ReportingManager;


                        // ==========================================
                        // 20. SHIFT
                        // ==========================================

                        ws.Cells[row, 20].Value =
                            emp.Shift;


                        // ==========================================
                        // 21. AADHAAR NUMBER
                        // ==========================================

                        ws.Cells[row, 21].Value =
                            emp.AadhaarNumber;


                        // ==========================================
                        // 22. PAN NUMBER
                        // ==========================================

                        ws.Cells[row, 22].Value =
                            emp.PANNumber;


                        // ==========================================
                        // 23. BANK NAME
                        // ==========================================

                        ws.Cells[row, 23].Value =
                            emp.BankName;


                        // ==========================================
                        // 24. ACCOUNT NUMBER
                        // ==========================================

                        ws.Cells[row, 24].Value =
                            emp.AccountNumber;


                        // ==========================================
                        // 25. IFSC CODE
                        // ==========================================

                        ws.Cells[row, 25].Value =
                            emp.IFSCCode;


                        // ==========================================
                        // 26. CREATED DATE
                        // ==========================================

                        ws.Cells[row, 26].Value =
                            emp.CreatedDate.ToString("dd-MM-yyyy");


                        // ==========================================
                        // 27. UPDATED DATE
                        // ==========================================

                        ws.Cells[row, 27].Value =
                            emp.UpdatedDate.ToString("dd-MM-yyyy");


                        row++;
                    }


                    // ==========================================
                    // AUTO FIT COLUMNS
                    // ==========================================

                    ws.Cells[ws.Dimension.Address]
                        .AutoFitColumns();


                    // ==========================================
                    // GENERATE EXCEL FILE
                    // ==========================================

                    byte[] file =
                        await package.GetAsByteArrayAsync();


                    // ==========================================
                    // DOWNLOAD FILE
                    // ==========================================

                    return File(
                        file,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"EmployeeMaster_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                TempData["Error"] =
                    "Something went wrong while exporting employee data.";

                return RedirectToAction("Employees");
            }
        }
       
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

        
    }
}
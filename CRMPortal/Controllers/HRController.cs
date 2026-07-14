using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

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
                if (HttpContext.Session.GetInt32("RoleId") != 4)
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

               

                model.EmployeeList = _context.MasterEmployee
                                             .Where(x => !x.IsDeleted)
                                             .ToList();

                

                // Total Employees
                ViewBag.TotalEmployees =
                    _context.MasterEmployee
                            .Count(x => x.IsDeleted == false);

                // Active Employees
                ViewBag.ActiveEmployees =
                    _context.MasterEmployee
                            .Count(x => x.EmployeeStatus == "Active"
                                     && x.IsDeleted == false);

                // Inactive Employees
                ViewBag.InactiveEmployees =
                    _context.MasterEmployee
                            .Count(x => x.EmployeeStatus == "Inactive"
                                     && x.IsDeleted == false);

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

                if(HttpContext.Session.GetInt32("RoleId") != 4)

                {
                    return RedirectToAction("Login", "Account");
                }

                var employees = _context.MasterEmployee
                                .Where(x => !x.IsDeleted)
                                .OrderByDescending(x => x.EmployeeCode)
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

        public IActionResult EditEmployee(int id)
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

            HRDashboardViewModel vm = new HRDashboardViewModel();

            vm.MasterEmployee = employee;

            vm.EmployeeAttendance = new EmployeeAttendance();

            vm.EmployeeList = _context.MasterEmployee
                                      .Where(x => !x.IsDeleted)
                                      .OrderBy(x => x.FullName)
                                      .ToList();

            ViewBag.TotalEmployees =
                _context.MasterEmployee.Count(x => !x.IsDeleted);

            ViewBag.ActiveEmployees =
                _context.MasterEmployee.Count(x =>
                    x.EmployeeStatus == "Active" &&
                    !x.IsDeleted);

            ViewBag.InactiveEmployees =
                _context.MasterEmployee.Count(x =>
                    x.EmployeeStatus == "Inactive" &&
                    !x.IsDeleted);

            return View("Dashboard", vm);
        }

        // Update employee Method 

        [HttpPost]
        public IActionResult UpdateEmployee(HRDashboardViewModel model)
        {
            var employee = model.MasterEmployee;

            var dbEmployee = _context.MasterEmployee
                .FirstOrDefault(x => x.EmployeeId == employee.EmployeeId);

            if (dbEmployee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("Employees");
            }

            // Duplicate Employee Code
            bool employeeCodeExists = _context.MasterEmployee.Any(x =>
                x.EmployeeCode == employee.EmployeeCode &&
                x.EmployeeId != employee.EmployeeId &&
                !x.IsDeleted);

            if (employeeCodeExists)
            {
                TempData["Error"] = "Employee Code already exists.";
                return RedirectToAction("Dashboard");
            }

            // Duplicate Full Name
            bool employeeNameExists = _context.MasterEmployee.Any(x =>
                x.FullName == employee.FullName &&
                x.EmployeeId != employee.EmployeeId &&
                !x.IsDeleted);

            if (employeeNameExists)
            {
                TempData["Error"] = "Employee Name already exists.";
                return RedirectToAction("Dashboard");
            }

            dbEmployee.EmployeeCode = employee.EmployeeCode;
            dbEmployee.FirstName = employee.FirstName;
            dbEmployee.LastName = employee.LastName;
            dbEmployee.FullName = employee.FullName;
            dbEmployee.Email = employee.Email;
            dbEmployee.MobileNumber = employee.MobileNumber;
            dbEmployee.AlternateMobile = employee.AlternateMobile;
            dbEmployee.Gender = employee.Gender;
            dbEmployee.DateOfBirth = employee.DateOfBirth;
            dbEmployee.BloodGroup = employee.BloodGroup;
            dbEmployee.MaritalStatus = employee.MaritalStatus;
            dbEmployee.EmergencyContactName = employee.EmergencyContactName;
            dbEmployee.EmergencyContact = employee.EmergencyContact;
            dbEmployee.AddressLine1 = employee.AddressLine1;
            dbEmployee.City = employee.City;
            dbEmployee.State = employee.State;
            dbEmployee.Country = employee.Country;
            dbEmployee.Pincode = employee.Pincode;
            dbEmployee.Department = employee.Department;
            dbEmployee.Designation = employee.Designation;
            dbEmployee.JoiningDate = employee.JoiningDate;
            dbEmployee.EmploymentType = employee.EmploymentType;
            dbEmployee.ReportingManager = employee.ReportingManager;
            dbEmployee.WorkLocation = employee.WorkLocation;
            dbEmployee.Shift = employee.Shift;
            dbEmployee.Salary = employee.Salary;
            dbEmployee.AadhaarNumber = employee.AadhaarNumber;
            dbEmployee.PANNumber = employee.PANNumber;
            dbEmployee.PassportNumber = employee.PassportNumber;
            dbEmployee.BankName = employee.BankName;
            dbEmployee.AccountNumber = employee.AccountNumber;
            dbEmployee.IFSCCode = employee.IFSCCode;
            dbEmployee.EmployeeStatus = employee.EmployeeStatus;

           
            // Copy remaining fields...

            dbEmployee.ModifiedDate = DateTime.Now;
            dbEmployee.UpdatedDate = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Employee Updated Successfully.";

            return RedirectToAction("Employees");
        }

        // Delete Employee Methods
        public IActionResult DeleteEmployee(int id)
        {
            try
            {
                var employee = _context.MasterEmployee.FirstOrDefault(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";

                    return RedirectToAction("Employees");
                }

                employee.IsDeleted = true;
                employee.ModifiedDate = DateTime.Now;

                _context.SaveChanges();

                TempData["Success"] = "Employee deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Employees");
        }

        [HttpPost]
        public IActionResult AddEmployee(HRDashboardViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 4)
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
                    TempData["Error"] = "Employee Code already exists.";

                    return RedirectToAction("Dashboard");
                }

                // Check duplicate Full Name
                bool employeeNameExists = _context.MasterEmployee
                    .Any(x => x.FullName == employee.FullName &&
                              x.IsDeleted == false);

                if (employeeNameExists)
                {
                    TempData["Error"] = "Employee Name already exists.";

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
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var attendance = model.EmployeeAttendance;

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

                attendance.CreatedDate = DateTime.Now;
                attendance.UpdatedDate = DateTime.Now;
                attendance.ModifiedDate = DateTime.Now;
                attendance.IsDeleted = false;

                _context.EmployeeAttendance.Add(attendance);

                _context.SaveChanges();

                TempData["Success"] = "Attendance Saved Successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
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
                    ws.Cells[row, 2].Value = emp.FirstName;
                    ws.Cells[row, 3].Value = emp.LastName;
                    ws.Cells[row, 4].Value = emp.FullName;
                    ws.Cells[row, 5].Value = emp.Email;
                    ws.Cells[row, 6].Value = emp.Gender;
                    ws.Cells[row, 7].Value = emp.DateOfBirth.ToString("dd-MM-yyyy");
                    ws.Cells[row, 8].Value = emp.BloodGroup;
                    ws.Cells[row, 9].Value = emp.MaritalStatus;
                    ws.Cells[row, 10].Value = emp.MobileNumber;
                    ws.Cells[row, 11].Value = emp.AlternateMobile;
                    ws.Cells[row, 12].Value = emp.EmergencyContactName;
                    ws.Cells[row, 13].Value = emp.EmergencyContact;
                    ws.Cells[row, 14].Value = emp.AddressLine1;
                    ws.Cells[row, 15].Value = emp.City;
                    ws.Cells[row, 16].Value = emp.State;
                    ws.Cells[row, 17].Value = emp.Country;
                    ws.Cells[row, 18].Value = emp.Pincode;
                    ws.Cells[row, 19].Value = emp.Department;
                    ws.Cells[row, 20].Value = emp.Designation;
                    ws.Cells[row, 21].Value = emp.JoiningDate.ToString("dd-MM-yyyy");
                    ws.Cells[row, 22].Value = emp.EmploymentType;
                    ws.Cells[row, 23].Value = emp.ReportingManager;
                    ws.Cells[row, 24].Value = emp.WorkLocation;
                    ws.Cells[row, 25].Value = emp.Shift;
                    ws.Cells[row, 26].Value = emp.Salary;
                    ws.Cells[row, 27].Value = emp.AadhaarNumber;
                    ws.Cells[row, 28].Value = emp.PANNumber;
                    ws.Cells[row, 29].Value = emp.PassportNumber;
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
            model.EmployeeList = _context.MasterEmployee
                                         .Where(x => !x.IsDeleted)
                                         .OrderBy(x => x.FullName)
                                         .ToList();

            model.AttendanceList = _context.EmployeeAttendance
                .Where(x =>
                    x.EmployeeId == model.EmployeeId &&
                    x.AttendanceDate >= model.FromDate &&
                    x.AttendanceDate <= model.ToDate &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.AttendanceDate)
                .ToList();

            // Get Employee Salary

            var employee = _context.MasterEmployee
                .FirstOrDefault(x =>
                    x.EmployeeId == model.EmployeeId);

            if (employee != null)
            {
                model.MonthlySalary = employee.Salary;
            }


            // Total Calendar Days
            // Total Calendar Days

            model.TotalDays =
                model.ToDate.DayNumber -
                model.FromDate.DayNumber + 1;

            // Count Saturdays & Sundays

            int saturday = 0;
            int sunday = 0;

            for (DateOnly date = model.FromDate;
                date <= model.ToDate;
                date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    saturday++;

                if (date.DayOfWeek == DayOfWeek.Sunday)
                    sunday++;
            }

            model.SaturdayCount = saturday;

            model.SundayCount = sunday;

            // Working Days

            model.WorkingDays =
                model.TotalDays -
                saturday -
                sunday;


            // Present Count
            model.PresentDays =
                model.AttendanceList
                .Count(x => x.Status == "Present");


            // Absent Count
            model.AbsentDays =
                model.AttendanceList
                .Count(x => x.Status == "Absent");


            // Attendance Percentage
            if (model.WorkingDays > 0)
            {
                model.AttendancePercentage =
                    Math.Round(
                        (double)model.PresentDays /
                        model.WorkingDays * 100,
                        2);
            }

            if (model.WorkingDays > 0)
            {
                model.PerDaySalary =
                    Math.Round(
                        model.MonthlySalary /
                        model.WorkingDays,
                        2);

                model.SalaryDeduction =
                    Math.Round(
                        model.PerDaySalary *
                        model.AbsentDays,
                        2);

                model.NetSalary =
                    model.MonthlySalary -
                    model.SalaryDeduction;
            }
            return View(model);
        }
    }
}

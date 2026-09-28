using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.Services;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRMPortal.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EmployeeController(AppDbContext context, IWebHostEnvironment environment )
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Dashboard()
        {
            try
            {
                // Check Employee Role
                if (HttpContext.Session.GetInt32("RoleId") != 2)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get EmployeeId from Session
                int employeeId = HttpContext.Session
                    .GetInt32("EmployeeId").Value;

                // Get Employee
                var employee = _context.MasterEmployee
                    .FirstOrDefault(x =>
                        x.EmployeeId == employeeId &&
                        !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Login", "Account");
                }

                // Today's date
                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);

                // Get today's login tracker
                var login = _context.EmployeeLoginTracker
                    .FirstOrDefault(x =>
                        x.EmployeeId == employeeId &&
                        x.LoginDate == today &&
                        !x.IsDeleted);

                if (login != null)
                {
                    // Sign In Time
                    ViewBag.SignInTime =
                        login.SignInTime?.ToString("hh:mm tt");

                    // Sign Out Time
                    ViewBag.SignOutTime =
                        login.SignOutTime?.ToString("hh:mm tt");

                    // Session Status
                    ViewBag.SessionStatus =
                        login.SessionStatus;

                    // Sign In DateTime for JavaScript Timer
                    if (login.SignInTime.HasValue)
                    {
                        ViewBag.SignInDateTime =
                            login.SignInTime.Value
                                .ToString("yyyy-MM-ddTHH:mm:ss");
                    }
                    else
                    {
                        ViewBag.SignInDateTime = "";
                    }
                }
                else
                {
                    // No attendance record for today

                    ViewBag.SignInTime = null;

                    ViewBag.SignOutTime = null;

                    ViewBag.SessionStatus =
                        "Not Signed In";

                    ViewBag.SignInDateTime = "";
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Login", "Account");
            }
        }

        // Session SignIn/ SignOut code 

        [HttpPost]
        public IActionResult EmployeeSignIn()
        {
            try
            {
                // Get EmployeeId from Session
                int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                bool alreadySignedIn = _context.EmployeeLoginTracker.Any(x =>
                    x.EmployeeId == employeeId &&
                    x.LoginDate == today &&
                    !x.IsDeleted);

                if (alreadySignedIn)
                {
                    TempData["Error"] = "You have already signed in today.";
                    return RedirectToAction("Dashboard");
                }

                var employee = _context.MasterEmployee
                    .FirstOrDefault(x => x.EmployeeId == employeeId && !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction("Login", "Account");
                }

                EmployeeLoginTracker tracker = new EmployeeLoginTracker();

                tracker.EmployeeId = employee.EmployeeId;
                tracker.EmployeeCode = employee.EmployeeCode;
                tracker.FullName = employee.FullName;
                tracker.LoginDate = today;
                tracker.SignInTime = DateTime.Now;
                tracker.SessionStatus = "Signed In";
                tracker.CreatedDate = DateTime.Now;
                tracker.UpdatedDate = DateTime.Now;
                tracker.ModifiedDate = DateTime.Now;
                tracker.IsDeleted = false;

                _context.EmployeeLoginTracker.Add(tracker);
                _context.SaveChanges();

                TempData["Success"] = "Sign In recorded successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult EmployeeSignOut()
        {
            try
            {
                // Get EmployeeId from Session
                int employeeId = HttpContext.Session.GetInt32("EmployeeId").Value;

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                var tracker = _context.EmployeeLoginTracker
                    .FirstOrDefault(x =>
                        x.EmployeeId == employeeId &&
                        x.LoginDate == today &&
                        !x.IsDeleted);

                if (tracker == null)
                {
                    TempData["Error"] = "Please Sign In first.";
                    return RedirectToAction("Dashboard");
                }

                if (tracker.SignOutTime != null)
                {
                    TempData["Error"] = "You have already signed out today.";
                    return RedirectToAction("Dashboard");
                }

                // Update Sign Out Time
                tracker.SignOutTime = DateTime.Now;

                // Update Status
                tracker.SessionStatus = "Signed Out";

                // Calculate Working Hours
                tracker.TotalWorkingHours = Convert.ToDecimal(
                    Math.Round(
                        (tracker.SignOutTime.Value - tracker.SignInTime.Value).TotalHours,
                        2));

                tracker.UpdatedDate = DateTime.Now;
                tracker.ModifiedDate = DateTime.Now;

                _context.SaveChanges();

                TempData["Success"] = "Sign Out recorded successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");
            }
        }


        // ================= Get Apply Leave =================


        [HttpGet]
        public IActionResult ApplyLeave()
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 2)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = HttpContext.Session.GetInt32("UserId").Value;

                ApplyLeaveViewModel model = new ApplyLeaveViewModel();

                // Leave Type Dropdown
                model.LeaveTypes = _context.LeaveTypes
                    .Select(x => new SelectListItem
                    {
                        Value = x.LeaveTypeId.ToString(),
                        Text = x.LeaveTypeName
                    })
                    .ToList();

                // Leave History
                model.MyLeaves = _context.LeaveRequests
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToList();

                // ==========================================
                // LEAVE BALANCE CALCULATION
                // Company Policy: 1.5 Paid Leave every month
                // ==========================================

                int currentYear = DateTime.Now.Year;
                int currentMonth = DateTime.Now.Month;

                model.MonthlyLeaveEarned = 1.5M;

                // Total PL earned till current month
                model.AnnualLeaveBalance = currentMonth * 1.5M;

                // Approved Paid Leave (Casual + Sick Leave only)
                model.LeaveUsedThisYear = _context.LeaveRequests
                    .Where(x =>
                        x.UserId == userId &&
                        x.Status == "Approved" &&
                        x.FromDate.Year == currentYear &&
                        (x.LeaveTypeId == 1 || x.LeaveTypeId == 2))
                    .Sum(x => (decimal?)x.TotalDays) ?? 0;

                // Remaining PL Balance
                model.RemainingLeaveBalance =
                    model.AnnualLeaveBalance - model.LeaveUsedThisYear;

                if (model.RemainingLeaveBalance < 0)
                {
                    model.RemainingLeaveBalance = 0;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");
            }
        }


        [HttpPost]
        public IActionResult ApplyLeave(ApplyLeaveViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                if (model.FromDate < today || model.ToDate < today)
                {
                    TempData["Error"] = "Leave dates cannot be earlier than today.";

                    return RedirectToAction("ApplyLeave");
                }

                if (model.ToDate < model.FromDate)
                {
                    TempData["Error"] = "To Date cannot be earlier than From Date.";

                    return RedirectToAction("ApplyLeave");
                }

                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));

                decimal totalDays =
                    model.ToDate.DayNumber -
                    model.FromDate.DayNumber + 1;

                LeaveRequests leaveRequest =
                    new LeaveRequests
                    {
                        UserId = userId,

                        LeaveTypeId = model.LeaveTypeId,

                        FromDate = model.FromDate,

                        ToDate = model.ToDate,

                        TotalDays = totalDays,

                        Reason = model.Reason,

                        Status = "Pending",

                        CreatedDate = DateTime.Now,

                        UpdatedDate = DateTime.Now
                    };

                _context.LeaveRequests.Add(leaveRequest);

                _context.SaveChanges();

                /*var adminUsers = _context.Users.Where(x => x.RoleId == 1).ToList();

                foreach (var admin in adminUsers)
                {
                    string subject =
                        "New Leave Request Submitted";

                    string body =
                        $"Employee has applied for leave.\n\n" +
                        $"Employee ID: {userId}\n" +
                        $"From Date: {model.FromDate}\n" +
                        $"To Date: {model.ToDate}\n" +
                        $"Reason: {model.Reason}\n\n" +
                        $"Please login to CRM and review.";

                    _emailService.SendEmail(
                        admin.Email,
                        subject,
                        body);
                }*/

                TempData["Success"] = "Leave request submitted successfully.";

                return RedirectToAction("ApplyLeave");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("ApplyLeave");
            }
        }

        // ================= Upload file SEction =================

        [HttpPost]
        public IActionResult UploadCD(IFormFile File)
        {
            try
            {
                if (File == null || File.Length == 0)
                {
                    TempData["Error"] = "Please select a file.";

                    return RedirectToAction("Dashboard");
                }

                string extension = Path.GetExtension(File.FileName).ToLower();

                if (extension != ".xlsx" &&
                    extension != ".xls")
                {
                    TempData["Error"] = "Only Excel files (.xlsx or .xls) are allowed.";

                    return RedirectToAction("Dashboard");
                }

                int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

                string folderPath = Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "CDFiles");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + File.FileName;

                string fullPath =Path.Combine( folderPath,fileName);

                using (var stream =new FileStream(fullPath, FileMode.Create))
                {
                    File.CopyTo(stream);
                }

                EmployeeFiles employeeFile = new EmployeeFiles
                    {
                        UserId = userId,
                        FileName = File.FileName,
                        FilePath = fileName,
                        FileSize = File.Length,
                        UploadedDate = DateTime.Now
                    };

                _context.EmployeeFiles.Add(employeeFile);

                _context.SaveChanges();

                TempData["Success"] = "File Uploaded Successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }
        }

        // ================= Upload Delete Section =================

        public IActionResult DeleteFile(int id)
        {
            try
            {
                if(HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(HttpContext.Session.GetInt32("UserId"));

                var file = _context.EmployeeFiles
                    .FirstOrDefault(x =>
                        x.FileId == id &&
                        x.UserId == userId);

                if (file == null)
                {
                    TempData["Error"] =
                        "File not found.";

                    return RedirectToAction("Dashboard");
                }

                string fullPath =
            Path.Combine(
                _environment.WebRootPath,
                "Uploads",
                "CDFiles",
                file.FilePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.EmployeeFiles.Remove(file);

                _context.SaveChanges();

                TempData["Success"] =
                    "File deleted successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");

            }
        }
    }
}
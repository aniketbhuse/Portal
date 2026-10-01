using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.Services;
using CRMPortal.ViewModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CRMPortal.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ErrorLogService _errorLogService;

        public EmployeeController(
            AppDbContext context,
            IWebHostEnvironment environment,
            ErrorLogService errorLogService)
        {
            _context = context;
            _environment = environment;
            _errorLogService = errorLogService;
        }


        // =========================================================
        // EMPLOYEE DASHBOARD
        // =========================================================

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // ==========================================
                // CHECK EMPLOYEE ROLE
                // ==========================================

                if (HttpContext.Session.GetInt32("RoleId") != 2)
                {
                    return RedirectToAction("Login", "Account");
                }


                // ==========================================
                // GET EMPLOYEE ID FROM SESSION
                // ==========================================

                int? employeeIdSession =
                    HttpContext.Session.GetInt32("EmployeeId");

                if (!employeeIdSession.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                int employeeId = employeeIdSession.Value;


                // ==========================================
                // GET EMPLOYEE
                // ==========================================

                var employee = await _context.MasterEmployee
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";

                    return RedirectToAction("Login", "Account");
                }


                // ==========================================
                // TODAY'S DATE
                // ==========================================

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);


                // ==========================================
                // GET TODAY'S LOGIN TRACKER
                // ==========================================

                var login = await _context.EmployeeLoginTracker
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.LoginDate == today &&
                        !x.IsDeleted);


                // ==========================================
                // IF TODAY'S RECORD EXISTS
                // ==========================================

                if (login != null)
                {
                    // ------------------------------
                    // SIGN IN TIME
                    // ------------------------------

                    ViewBag.SignInTime =
                        login.SignInTime?.ToString("hh:mm tt");


                    // ------------------------------
                    // SIGN OUT TIME
                    // ------------------------------

                    ViewBag.SignOutTime =
                        login.SignOutTime?.ToString("hh:mm tt");


                    // ------------------------------
                    // SESSION STATUS
                    // ------------------------------

                    ViewBag.SessionStatus =
                        login.SessionStatus;


                    // ------------------------------
                    // TOTAL WORKING HOURS
                    // ------------------------------

                    ViewBag.TotalWorkingHours =
                        login.TotalWorkingHours;


                    // ------------------------------
                    // SIGN IN DATETIME
                    // Used by JavaScript timer
                    // ------------------------------

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


                    // ------------------------------
                    // SIGN OUT DATETIME
                    // Used for 1-hour UI reset
                    // ------------------------------

                    if (login.SignOutTime.HasValue)
                    {
                        ViewBag.SignOutDateTime =
                            login.SignOutTime.Value
                                .ToString("yyyy-MM-ddTHH:mm:ss");
                    }
                    else
                    {
                        ViewBag.SignOutDateTime = "";
                    }
                }
                else
                {
                    // ==========================================
                    // NO RECORD FOR TODAY
                    // ==========================================

                    ViewBag.SignInTime = "";

                    ViewBag.SignOutTime = "";

                    ViewBag.SessionStatus =
                        "Not Signed In";

                    ViewBag.TotalWorkingHours = "";

                    ViewBag.SignInDateTime = "";

                    ViewBag.SignOutDateTime = "";
                }


                return View(employee);
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(Dashboard));

                TempData["Error"] =
                    "Something went wrong while loading the dashboard.";

                return RedirectToAction("Login", "Account");
            }
        }


        // =========================================================
        // EMPLOYEE SIGN IN
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> EmployeeSignIn()
        {
            try
            {
                // ==========================================
                // Get EmployeeId from Session
                // ==========================================

                int? employeeIdSession =
                    HttpContext.Session.GetInt32("EmployeeId");

                if (!employeeIdSession.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                int employeeId =
                    employeeIdSession.Value;


                // ==========================================
                // TODAY
                // ==========================================

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);


                // ==========================================
                // CHECK ALREADY SIGNED IN
                // ==========================================

                bool alreadySignedIn =
                    await _context.EmployeeLoginTracker
                        .AnyAsync(x =>
                            x.EmployeeId == employeeId &&
                            x.LoginDate == today &&
                            !x.IsDeleted);

                if (alreadySignedIn)
                {
                    TempData["Error"] =
                        "You have already signed in today.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // GET EMPLOYEE
                // ==========================================

                var employee =
                    await _context.MasterEmployee
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employeeId &&
                            !x.IsDeleted);

                if (employee == null)
                {
                    TempData["Error"] =
                        "Employee not found.";

                    return RedirectToAction("Login", "Account");
                }


                // ==========================================
                // CREATE LOGIN TRACKER
                // ==========================================

                EmployeeLoginTracker tracker =
                    new EmployeeLoginTracker();

                tracker.EmployeeId =
                    employee.EmployeeId;

                tracker.EmployeeCode =
                    employee.EmployeeCode;

                tracker.FullName =
                    employee.FullName;

                tracker.LoginDate =
                    today;

                tracker.SignInTime =
                    DateTime.Now;

                tracker.SessionStatus =
                    "Signed In";

                tracker.CreatedDate =
                    DateTime.Now;

                tracker.UpdatedDate =
                    DateTime.Now;

                tracker.ModifiedDate =
                    DateTime.Now;

                tracker.IsDeleted =
                    false;


                // ==========================================
                // SAVE
                // ==========================================

                _context.EmployeeLoginTracker.Add(tracker);

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Sign In recorded successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(EmployeeSignIn));

                TempData["Error"] =
                    "Something went wrong while signing in.";

                return RedirectToAction("Dashboard");
            }
        }


        // =========================================================
        // EMPLOYEE SIGN OUT
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> EmployeeSignOut()
        {
            try
            {
                // ==========================================
                // GET EMPLOYEE ID FROM SESSION
                // ==========================================

                int? employeeIdSession =
                    HttpContext.Session.GetInt32("EmployeeId");

                if (!employeeIdSession.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                int employeeId =
                    employeeIdSession.Value;


                // ==========================================
                // TODAY
                // ==========================================

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);


                // ==========================================
                // GET TODAY'S TRACKER
                // ==========================================

                var tracker =
                    await _context.EmployeeLoginTracker
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employeeId &&
                            x.LoginDate == today &&
                            !x.IsDeleted);

                if (tracker == null)
                {
                    TempData["Error"] =
                        "Please Sign In first.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // ALREADY SIGNED OUT
                // ==========================================

                if (tracker.SignOutTime != null)
                {
                    TempData["Error"] =
                        "You have already signed out today.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // SIGN OUT TIME
                // ==========================================

                DateTime signOutTime =
                    DateTime.Now;

                tracker.SignOutTime =
                    signOutTime;


                // ==========================================
                // SESSION STATUS
                // ==========================================

                tracker.SessionStatus =
                    "Signed Out";


                // ==========================================
                // CALCULATE WORKING HOURS
                // ==========================================

                if (tracker.SignInTime.HasValue)
                {
                    tracker.TotalWorkingHours =
                        Convert.ToDecimal(
                            Math.Round(
                                (
                                    signOutTime -
                                    tracker.SignInTime.Value
                                ).TotalHours,
                                2));
                }


                // ==========================================
                // UPDATE DATES
                // ==========================================

                tracker.UpdatedDate =
                    DateTime.Now;

                tracker.ModifiedDate =
                    DateTime.Now;


                // ==========================================
                // SAVE
                // ==========================================

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Sign Out recorded successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(EmployeeSignOut));

                TempData["Error"] =
                    "Something went wrong while signing out.";

                return RedirectToAction("Dashboard");
            }
        }


        // =========================================================
        // GET APPLY LEAVE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ApplyLeave()
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 2)
                {
                    return RedirectToAction("Login", "Account");
                }


                int? userIdSession =
                    HttpContext.Session.GetInt32("UserId");

                if (!userIdSession.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId =
                    userIdSession.Value;


                ApplyLeaveViewModel model =
                    new ApplyLeaveViewModel();


                // ==========================================
                // LEAVE TYPE DROPDOWN
                // ==========================================

                model.LeaveTypes =
                    await _context.LeaveTypes
                        .Select(x => new SelectListItem
                        {
                            Value =
                                x.LeaveTypeId.ToString(),

                            Text =
                                x.LeaveTypeName
                        })
                        .ToListAsync();


                // ==========================================
                // LEAVE HISTORY
                // ==========================================

                model.MyLeaves =
                    await _context.LeaveRequests
                        .Where(x =>
                            x.UserId == userId)
                        .OrderByDescending(x =>
                            x.CreatedDate)
                        .ToListAsync();


                // ==========================================
                // LEAVE BALANCE CALCULATION
                // Company Policy: 1.5 Paid Leave every month
                // ==========================================

                int currentYear =
                    DateTime.Now.Year;

                int currentMonth =
                    DateTime.Now.Month;


                model.MonthlyLeaveEarned =
                    1.5M;


                // ==========================================
                // TOTAL PL EARNED TILL CURRENT MONTH
                // ==========================================

                model.AnnualLeaveBalance =
                    currentMonth * 1.5M;


                // ==========================================
                // APPROVED PAID LEAVE
                // Casual + Sick Leave only
                // ==========================================

                model.LeaveUsedThisYear =
                    await _context.LeaveRequests
                        .Where(x =>
                            x.UserId == userId &&
                            x.Status == "Approved" &&
                            x.FromDate.Year == currentYear &&
                            (
                                x.LeaveTypeId == 1 ||
                                x.LeaveTypeId == 2
                            ))
                        .SumAsync(x =>
                            (decimal?)x.TotalDays) ?? 0;


                // ==========================================
                // REMAINING PL BALANCE
                // ==========================================

                model.RemainingLeaveBalance =
                    model.AnnualLeaveBalance -
                    model.LeaveUsedThisYear;


                if (model.RemainingLeaveBalance < 0)
                {
                    model.RemainingLeaveBalance = 0;
                }


                return View(model);
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(ApplyLeave));

                TempData["Error"] =
                    "Something went wrong while loading the leave page.";

                return RedirectToAction("Dashboard");
            }
        }


        // =========================================================
        // POST APPLY LEAVE
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> ApplyLeave(
            ApplyLeaveViewModel model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction("Login", "Account");
                }


                // ==========================================
                // TODAY
                // ==========================================

                DateOnly today =
                    DateOnly.FromDateTime(DateTime.Today);


                // ==========================================
                // DATE VALIDATION
                // ==========================================

                if (model.FromDate < today ||
                    model.ToDate < today)
                {
                    TempData["Error"] =
                        "Leave dates cannot be earlier than today.";

                    return RedirectToAction("ApplyLeave");
                }


                if (model.ToDate < model.FromDate)
                {
                    TempData["Error"] =
                        "To Date cannot be earlier than From Date.";

                    return RedirectToAction("ApplyLeave");
                }


                // ==========================================
                // GET USER ID
                // ==========================================

                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));


                // ==========================================
                // TOTAL DAYS
                // ==========================================

                decimal totalDays =
                    model.ToDate.DayNumber -
                    model.FromDate.DayNumber +
                    1;


                // ==========================================
                // CREATE LEAVE REQUEST
                // ==========================================

                LeaveRequests leaveRequest =
                    new LeaveRequests
                    {
                        UserId = userId,

                        LeaveTypeId =
                            model.LeaveTypeId,

                        FromDate =
                            model.FromDate,

                        ToDate =
                            model.ToDate,

                        TotalDays =
                            totalDays,

                        Reason =
                            model.Reason,

                        Status =
                            "Pending",

                        CreatedDate =
                            DateTime.Now,

                        UpdatedDate =
                            DateTime.Now
                    };


                _context.LeaveRequests.Add(
                    leaveRequest);


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "Leave request submitted successfully.";

                return RedirectToAction("ApplyLeave");
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(ApplyLeave));

                TempData["Error"] =
                    "Something went wrong while submitting the leave request.";

                return RedirectToAction("ApplyLeave");
            }
        }


        // =========================================================
        // UPLOAD FILE
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> UploadCD(
            IFormFile File)
        {
            try
            {
                // ==========================================
                // FILE VALIDATION
                // ==========================================

                if (File == null ||
                    File.Length == 0)
                {
                    TempData["Error"] =
                        "Please select a file.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // EXTENSION VALIDATION
                // ==========================================

                string extension =
                    Path.GetExtension(
                        File.FileName)
                        .ToLower();


                if (extension != ".xlsx" &&
                    extension != ".xls")
                {
                    TempData["Error"] =
                        "Only Excel files (.xlsx or .xls) are allowed.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // USER ID
                // ==========================================

                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));


                // ==========================================
                // FOLDER PATH
                // ==========================================

                string folderPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "CDFiles");


                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(
                        folderPath);
                }


                // ==========================================
                // FILE NAME
                // ==========================================

                string fileName =
                    Guid.NewGuid().ToString() +
                    "_" +
                    File.FileName;


                string fullPath =
                    Path.Combine(
                        folderPath,
                        fileName);


                // ==========================================
                // SAVE FILE ASYNC
                // ==========================================

                using (var stream =
                       new FileStream(
                           fullPath,
                           FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }


                // ==========================================
                // SAVE FILE INFORMATION
                // ==========================================

                EmployeeFiles employeeFile =
                    new EmployeeFiles
                    {
                        UserId =
                            userId,

                        FileName =
                            File.FileName,

                        FilePath =
                            fileName,

                        FileSize =
                            File.Length,

                        UploadedDate =
                            DateTime.Now
                    };


                _context.EmployeeFiles.Add(
                    employeeFile);


                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "File Uploaded Successfully.";

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(UploadCD));

                TempData["Error"] =
                    "Something went wrong while uploading the file.";

                return RedirectToAction("Dashboard");
            }
        }


        // =========================================================
        // DELETE FILE
        // =========================================================

        public async Task<IActionResult> DeleteFile(
            int id)
        {
            try
            {
                // ==========================================
                // CHECK USER SESSION
                // ==========================================

                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction(
                        "Login",
                        "Account");
                }


                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));


                // ==========================================
                // GET FILE
                // ==========================================

                var file =
                    await _context.EmployeeFiles
                        .FirstOrDefaultAsync(x =>
                            x.FileId == id &&
                            x.UserId == userId);


                if (file == null)
                {
                    TempData["Error"] =
                        "File not found.";

                    return RedirectToAction(
                        "Dashboard");
                }


                // ==========================================
                // FILE PATH
                // ==========================================

                string fullPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "CDFiles",
                        file.FilePath);


                // ==========================================
                // DELETE PHYSICAL FILE
                // ==========================================

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }


                // ==========================================
                // DELETE DATABASE RECORD
                // ==========================================

                _context.EmployeeFiles.Remove(file);

                await _context.SaveChangesAsync();


                TempData["Success"] =
                    "File deleted successfully.";

                return RedirectToAction(
                    "Dashboard");
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(EmployeeController),
                    nameof(DeleteFile));

                TempData["Error"] =
                    "Something went wrong while deleting the file.";

                return RedirectToAction(
                    "Dashboard");
            }
        }
    }
}
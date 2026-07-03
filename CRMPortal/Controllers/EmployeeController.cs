using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using CRMPortal.Services;

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
                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId =
            Convert.ToInt32(
                HttpContext.Session.GetInt32("UserId"));

                ViewBag.Files =
                    _context.EmployeeFiles
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.UploadedDate)
                    .ToList(); 

                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "Something went wrong.";

                return RedirectToAction("Login", "Account");
            }
        }

        // ================= Get Apply Leave =================

        [HttpGet]
        public IActionResult ApplyLeave()
        {
            try
            {
                if (HttpContext.Session.GetInt32("UserId") == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));

                ViewBag.LeaveTypes =
                    _context.LeaveTypes.ToList();

                ViewBag.LeaveRequests =
                    _context.LeaveRequests
                            .Where(x => x.UserId == userId)
                            .OrderByDescending(x => x.CreatedDate)
                            .ToList();

                return View();
            }
            catch (Exception)
            {
                TempData["Error"] = "Something went wrong.";

                return RedirectToAction(
                    "Dashboard",
                    "Employee");
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

                TempData["Success"] =
                    "Leave request submitted successfully.";

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
                    TempData["Error"] =
                        "Please select a file.";

                    return RedirectToAction("Dashboard");
                }

                string extension = Path.GetExtension(File.FileName).ToLower();

                if (extension != ".xlsx" &&
                    extension != ".xls")
                {
                    TempData["Error"] =
                        "Only Excel files (.xlsx or .xls) are allowed.";

                    return RedirectToAction("Dashboard");
                }

                int userId =
                    Convert.ToInt32(
                        HttpContext.Session.GetInt32("UserId"));

                string folderPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "CDFiles");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName =
                    Guid.NewGuid().ToString()
                    + "_"
                    + File.FileName;

                string fullPath =Path.Combine( folderPath,fileName);

                using (var stream =new FileStream(fullPath, FileMode.Create))
                {
                    File.CopyTo(stream);
                }

                EmployeeFiles employeeFile =
                    new EmployeeFiles
                    {
                        UserId = userId,
                        FileName = File.FileName,
                        FilePath = fileName,
                        FileSize = File.Length,
                        UploadedDate = DateTime.Now
                    };

                _context.EmployeeFiles.Add(employeeFile);

                _context.SaveChanges();

                TempData["Success"] =
                    "File Uploaded Successfully.";

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

                var file =
                    _context.EmployeeFiles
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
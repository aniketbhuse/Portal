using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using OfficeOpenXml;
using System.Data;
using Microsoft.AspNetCore.Hosting;

namespace CRMPortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
     AppDbContext context,
     IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ================= SESSION CHECK =================
        private bool IsAdminLoggedIn()
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");

            return HttpContext.Session.GetInt32("UserId") != null
                   && (roleId == 1 || roleId == 3);
        }

        // ================= LOAD DATA =================
        private void LoadAddUserData(AddUserViewModel model)
        {
            model.Roles = _context.Roles
                .Select(x => new SelectListItem
                {
                    Value = x.RoleId.ToString(),
                    Text = x.RoleName
                })
                .ToList();

            model.AdminUsers = _context.Users
    .Where(x => x.RoleId == 1 || x.RoleId == 3)
    .ToList();

            model.EmployeeUsers = _context.Users
                .Where(x => x.RoleId == 2)
                .ToList();
        }

        

        // ================= GET ADD USER =================
        public IActionResult AddUser()
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var model = new AddUserViewModel();

                LoadAddUserData(model);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public IActionResult LeaveRequests()
        {
            try
            {
                int? roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId != 1 && roleId != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                var leaves = (from lr  in _context.LeaveRequests 
                              join u in _context.Users
                              on lr.UserId equals u.UserId
                              join lt in _context.LeaveTypes
                              on lr.LeaveTypeId equals lt.LeaveTypeId
                               orderby lr.CreatedDate descending
                               
                               select new
                               {
                                   lr.LeaveRequestId,
                                   EmployeeName = u.FullName,
                                   lt.LeaveTypeName,
                                   lr.FromDate,
                                   lr.ToDate,
                                   lr.TotalDays,
                                   lr.Reason,
                                   lr.Status,
                                   lr.CreatedDate
                               }).ToList();

                ViewBag.Leaves = leaves;

                return View();
            }
            catch
            {
                

                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult ApproveLeave(
    int id,
    string remarks)
        {
            try
            {
                var leave =
                    _context.LeaveRequests
                    .FirstOrDefault(x =>
                        x.LeaveRequestId == id);

                if (leave != null)
                {
                    leave.Status = "Approved";

                    leave.AdminRemarks = remarks;

                    leave.ApprovedBy =
                        HttpContext.Session.GetInt32("UserId");

                    leave.ApprovedDate =
                        DateTime.Now;

                    leave.UpdatedDate =
                        DateTime.Now;

                    _context.SaveChanges();
                }

                TempData["Success"] =
                    "Leave Approved Successfully.";

                return RedirectToAction("LeaveRequests");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("LeaveRequests");
            }
        }


        [HttpPost]
        public IActionResult RejectLeave(int id, string remarks)
        {
            try
            {
                var leave = _context.LeaveRequests.FirstOrDefault(x => x.LeaveRequestId == id);

                if (leave != null)
                {
                    leave.Status = "Rejected";

                    leave.AdminRemarks = remarks;

                    leave.ApprovedBy =
                        HttpContext.Session.GetInt32("UserId");

                    leave.ApprovedDate =
                        DateTime.Now;

                    leave.UpdatedDate =
                        DateTime.Now;

                    _context.SaveChanges();
                }

                TempData["Success"] =
                    "Leave Rejected Successfully.";

                return RedirectToAction("LeaveRequests");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("LeaveRequests");
            }
        }


        // ================= POST ADD USER =================
        [HttpPost]
        public IActionResult AddUser(AddUserViewModel model)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                if (model.Users == null)
                {
                    TempData["Error"] = "Invalid form data";
                    return RedirectToAction("AddUser");
                }

                var existingUser = _context.Users.FirstOrDefault(x => x.Email == model.Users.Email);

                if (existingUser != null)
                {
                    ViewBag.Error = "Email already exists";
                    LoadAddUserData(model);
                    return View(model);
                }

                model.Users.CreatedDate = DateTime.Now;
                model.Users.UpdatedDate = DateTime.Now;

                _context.Users.Add(model.Users);
                _context.SaveChanges();

                TempData["Success"] = "User added Successfully";

                return RedirectToAction("AddUser");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                LoadAddUserData(model);
                return View(model);
            }
        }

        // ================= DELETE USER =================
        public IActionResult DeleteUser(int id)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var user = _context.Users.FirstOrDefault(x => x.UserId == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("AddUser");
                }

                _context.Users.Remove(user);
                _context.SaveChanges();

                TempData["Success"] = "User deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("AddUser");
        }

        public IActionResult Search(string query)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var model = new AddUserViewModel();

                LoadAddUserData(model);

                if (!string.IsNullOrEmpty(query))
                {
                    var result = _context.Users
                        .Where(x =>
                            x.FullName.Contains(query) || x.Email.Contains(query))
                        .ToList();

                    model.AdminUsers = result.Where(x => x.RoleId == 1).ToList();
                    model.EmployeeUsers = result.Where(x => x.RoleId == 2).ToList();
                }

                return View("AddUser", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // File Upload and View Code start from here

        public IActionResult Dashboard()
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction(
                        "Login",
                        "Account");
                }

                var files =
                    (from f in _context.EmployeeFiles
                     join u in _context.Users
                     on f.UserId equals u.UserId

                     orderby f.UploadedDate descending

                     select new
                     {
                         f.FileId,
                         EmployeeName = u.FullName,
                         f.FileName,
                         f.UploadedDate
                     }).ToList();

                ViewBag.Files = files;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction(
                    "Login",
                    "Account");
            }
        }

        [HttpGet]
        public IActionResult ViewFile(int id, int page = 1)
        {
            try
            {
                var file =
                    _context.EmployeeFiles
                    .FirstOrDefault(x => x.FileId == id);

                if (file == null)
                {
                    TempData["Error"] =
                        "File not found.";

                    return RedirectToAction(
                        "Dashboard");
                }

                string fullPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "CDFiles",
                        file.FilePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["Error"] =
                        "Physical file not found.";

                    return RedirectToAction(
                        "Dashboard");
                }

                ExcelPackage.License.SetNonCommercialPersonal("CRMPortal");

                using (var package = new ExcelPackage( new FileInfo(fullPath)))
                {
                    var sheet = package.Workbook.Worksheets[0];

                    int totalRows = sheet.Dimension.Rows;

                    int totalColumns = sheet.Dimension.Columns;

                    List<List<string>> allData = new List<List<string>>();

                    for (int row = 1;
                         row <= totalRows;
                         row++)
                    {
                        List<string> rowData =
                            new List<string>();

                        for (int col = 1;
                             col <= totalColumns;
                             col++)
                        {
                            rowData.Add(
                                sheet.Cells[row, col]
                                .Text);
                        }

                        allData.Add(rowData);
                    }
                    int pageSize = 10;

                    var headerRow = allData.FirstOrDefault();

                    int totalRecords =
                        allData.Count - 1;

                    var pagedData = allData
                        .Skip(((page - 1) * pageSize) + 1)
                        .Take(pageSize)
                        .ToList();

                    ViewBag.HeaderRow = headerRow;

                    ViewBag.FileId = id;

                    ViewBag.TotalRecords = totalRecords;

                    ViewBag.TotalColumns = totalColumns;

                    ViewBag.CurrentPage = page;

                    ViewBag.TotalPages = (int)Math.Ceiling((double) totalRecords / pageSize);

                    return View(pagedData);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;

                return RedirectToAction(
                    "Dashboard");
            }
        }
    }
}
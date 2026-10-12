using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.Services;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CRMPortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ErrorLogService _errorLogService;

        private const string ControllerName = "Admin";

        public AdminController(AppDbContext context, IWebHostEnvironment environment, ErrorLogService errorLogService)
        {
            _context = context;
            _environment = environment;
            _errorLogService = errorLogService;
        }

        // ================= ERROR LOG HELPER =================
        // Logging must never crash the request, so any failure inside the logger is swallowed.
        private async Task LogAsync(Exception ex, string action)
        {
            try
            {
                await _errorLogService.LogErrorAsync(ex, ControllerName, action);
            }
            catch
            {
                // ignore logging failures
            }
        }

        // ================= SESSION CHECK =================
        private bool IsAdminLoggedIn()
        {
            int? roleId = HttpContext.Session.GetInt32("RoleId");

            return HttpContext.Session.GetInt32("UserId") != null
                   && (roleId == 1 || roleId == 3);
        }

        // ================= LOAD DATA =================
        private async Task LoadAddUserData(AddUserViewModel model)
        {
            model.Roles = await _context.Roles
                .Where(x => x.RoleId != 3)   // Hide Super Admin role
                .Select(x => new SelectListItem
                {
                    Value = x.RoleId.ToString(),
                    Text = x.RoleName
                })
                .ToListAsync();

            model.AdminUsers = await _context.Users
                .Where(x => x.RoleId == 1)
                .ToListAsync();

            model.EmployeeUsers = await _context.Users
                .Where(x => x.RoleId == 2)
                .ToListAsync();
        }

        // ================= GET ADD USER =================
        [HttpGet]
        public async Task<IActionResult> AddUser()
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var model = new AddUserViewModel();

                await LoadAddUserData(model);

                return View(model);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(AddUser));
                TempData["Error"] = "Unable to load the user page. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        // ================= POST ADD USER =================
        [HttpPost]
        public async Task<IActionResult> AddUser(AddUserViewModel model)
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

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(x => x.Email == model.Users.Email);

                if (existingUser != null)
                {
                    ViewBag.Error = "Email already exists";
                    await LoadAddUserData(model);
                    return View(model);
                }

                model.Users.CreatedDate = DateTime.Now;
                model.Users.UpdatedDate = DateTime.Now;

                _context.Users.Add(model.Users);
                await _context.SaveChangesAsync();

                TempData["Success"] = "User added Successfully";

                return RedirectToAction("AddUser");
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(AddUser) + " (POST)");
                ViewBag.Error = "Unable to add the user. Please try again.";
                await LoadAddUserData(model);
                return View(model);
            }
        }

        // ================= GET EDIT USER =================
        // Loads the selected user into the top form of the AddUser page
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("AddUser");
                }

                // Super Admin is hidden everywhere else, so it can't be edited here either
                if (user.RoleId == 3)
                {
                    TempData["Error"] = "Super Admin cannot be edited.";
                    return RedirectToAction("AddUser");
                }

                var vm = new AddUserViewModel();
                await LoadAddUserData(vm);

                // Fresh form object (same type as AddUserViewModel.Users, needs a parameterless ctor)
                vm.Users = new();
                vm.Users.UserId = user.UserId;
                vm.Users.FullName = user.FullName;
                vm.Users.Email = user.Email;
                vm.Users.RoleId = user.RoleId;
                vm.Users.Password = string.Empty;   // never send the stored password back to the page

                return View("AddUser", vm);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(EditUser));
                TempData["Error"] = "Unable to load the user for editing. Please try again.";
                return RedirectToAction("AddUser");
            }
        }

        // ================= POST UPDATE USER =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(AddUserViewModel model)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                if (model.Users == null || model.Users.UserId <= 0)
                {
                    TempData["Error"] = "Invalid form data";
                    return RedirectToAction("AddUser");
                }

                // Manual validation (same style as AddUser, which doesn't rely on ModelState).
                // Password is optional while editing: blank = keep current password.
                if (string.IsNullOrWhiteSpace(model.Users.FullName) ||
                    string.IsNullOrWhiteSpace(model.Users.Email))
                {
                    return await ReloadEditForm(model, "Full name and email are required.");
                }

                // Super Admin role can't be assigned from this screen
                if (model.Users.RoleId == 3)
                {
                    return await ReloadEditForm(model, "This role cannot be assigned.");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.UserId == model.Users.UserId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("AddUser");
                }

                if (user.RoleId == 3)
                {
                    TempData["Error"] = "Super Admin cannot be edited.";
                    return RedirectToAction("AddUser");
                }

                // Email must stay unique (ignore the user being edited)
                bool emailTaken = await _context.Users.AnyAsync(x =>
                    x.Email == model.Users.Email && x.UserId != model.Users.UserId);

                if (emailTaken)
                {
                    return await ReloadEditForm(model, "Another user already uses this email.");
                }

                user.FullName = model.Users.FullName;
                user.Email = model.Users.Email;
                user.RoleId = model.Users.RoleId;
                user.UpdatedDate = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(model.Users.Password))
                {
                    // AddUser stores the password as typed, so this matches it.
                    // If you add hashing in AddUser, apply the same hashing here.
                    user.Password = model.Users.Password;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "User updated successfully.";
                return RedirectToAction("AddUser");
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(UpdateUser));
                return await ReloadEditForm(model, "Unable to update the user. Please try again.");
            }
        }

        // Re-shows the page in edit mode with the typed values + an error message
        private async Task<IActionResult> ReloadEditForm(AddUserViewModel posted, string error)
        {
            var vm = new AddUserViewModel();
            await LoadAddUserData(vm);

            vm.Users = posted.Users;
            ViewBag.Error = error;

            return View("AddUser", vm);
        }

        // ================= DELETE USER =================
        [HttpGet]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);

                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("AddUser");
                }

                // Don't allow deleting Super Admin
                if (user.RoleId == 3)
                {
                    TempData["Error"] = "Super Admin cannot be deleted.";
                    return RedirectToAction("AddUser");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = "User deleted successfully";
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(DeleteUser));
                TempData["Error"] = "Unable to delete the user. Please try again.";
            }

            return RedirectToAction("AddUser");
        }

        // ================= SEARCH =================
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            try
            {
                if (!IsAdminLoggedIn())
                    return RedirectToAction("Login", "Account");

                var model = new AddUserViewModel();

                await LoadAddUserData(model);

                if (!string.IsNullOrEmpty(query))
                {
                    var result = await _context.Users
                        .Where(x => x.FullName.Contains(query) || x.Email.Contains(query))
                        .ToListAsync();

                    model.AdminUsers = result.Where(x => x.RoleId == 1).ToList();
                    model.EmployeeUsers = result.Where(x => x.RoleId == 2).ToList();
                }

                return View("AddUser", model);
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(Search));
                TempData["Error"] = "Unable to search users. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        // ================= LEAVE REQUESTS =================
        [HttpGet]
        public async Task<IActionResult> LeaveRequests()
        {
            try
            {
                int? roleId = HttpContext.Session.GetInt32("RoleId");

                if (roleId != 1 && roleId != 3)
                {
                    return RedirectToAction("Login", "Account");
                }

                var leaves = await (from lr in _context.LeaveRequests
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
                                    }).ToListAsync();

                ViewBag.Leaves = leaves;

                return View();
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(LeaveRequests));
                TempData["Error"] = "Unable to load leave requests. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id, string remarks)
        {
            try
            {
                var leave = await _context.LeaveRequests
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == id);

                if (leave != null)
                {
                    leave.Status = "Approved";
                    leave.AdminRemarks = remarks;
                    leave.ApprovedBy = HttpContext.Session.GetInt32("UserId");
                    leave.ApprovedDate = DateTime.Now;
                    leave.UpdatedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Leave Approved Successfully.";

                return RedirectToAction("LeaveRequests");
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(ApproveLeave));
                TempData["Error"] = "Unable to approve the leave request. Please try again.";
                return RedirectToAction("LeaveRequests");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectLeave(int id, string remarks)
        {
            try
            {
                var leave = await _context.LeaveRequests
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == id);

                if (leave != null)
                {
                    leave.Status = "Rejected";
                    leave.AdminRemarks = remarks;
                    leave.ApprovedBy = HttpContext.Session.GetInt32("UserId");
                    leave.ApprovedDate = DateTime.Now;
                    leave.UpdatedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Leave Rejected Successfully.";

                return RedirectToAction("LeaveRequests");
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(RejectLeave));
                TempData["Error"] = "Unable to reject the leave request. Please try again.";
                return RedirectToAction("LeaveRequests");
            }
        }

        // ================= DASHBOARD (file list) =================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("Login", "Account");
                }

                var files = await (from f in _context.EmployeeFiles
                                   join u in _context.Users
                                       on f.UserId equals u.UserId
                                   orderby f.UploadedDate descending
                                   select new
                                   {
                                       f.FileId,
                                       EmployeeName = u.FullName,
                                       f.FileName,
                                       f.UploadedDate
                                   }).ToListAsync();

                ViewBag.Files = files;

                return View();
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(Dashboard));
                TempData["Error"] = "Unable to load the dashboard. Please try again.";
                return RedirectToAction("Login", "Account");
            }
        }

        // ================= VIEW FILE =================
        [HttpGet]
        public async Task<IActionResult> ViewFile(int id, int page = 1)
        {
            try
            {
                var file = await _context.EmployeeFiles
                    .FirstOrDefaultAsync(x => x.FileId == id);

                if (file == null)
                {
                    TempData["Error"] = "File not found.";
                    return RedirectToAction("Dashboard");
                }

                string fullPath = Path.Combine(
                    _environment.WebRootPath,
                    "Uploads",
                    "CDFiles",
                    file.FilePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    TempData["Error"] = "Physical file not found.";
                    return RedirectToAction("Dashboard");
                }

                ExcelPackage.License.SetNonCommercialPersonal("CRMPortal");

                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var package = new ExcelPackage())
                {
                    await package.LoadAsync(stream);

                    var sheet = package.Workbook.Worksheets[0];

                    int totalRows = sheet.Dimension.Rows;
                    int totalColumns = sheet.Dimension.Columns;

                    List<List<string>> allData = new List<List<string>>();

                    for (int row = 1; row <= totalRows; row++)
                    {
                        List<string> rowData = new List<string>();

                        for (int col = 1; col <= totalColumns; col++)
                        {
                            rowData.Add(sheet.Cells[row, col].Text);
                        }

                        allData.Add(rowData);
                    }

                    int pageSize = 10;

                    var headerRow = allData.FirstOrDefault();

                    int totalRecords = allData.Count - 1;

                    var pagedData = allData
                        .Skip(((page - 1) * pageSize) + 1)
                        .Take(pageSize)
                        .ToList();

                    ViewBag.HeaderRow = headerRow;
                    ViewBag.FileId = id;
                    ViewBag.TotalRecords = totalRecords;
                    ViewBag.TotalColumns = totalColumns;
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                    return View(pagedData);
                }
            }
            catch (Exception ex)
            {
                await LogAsync(ex, nameof(ViewFile));
                TempData["Error"] = "Unable to open the file. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }
    }
}
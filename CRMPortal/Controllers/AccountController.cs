
using CRMPortal.Data;
using CRMPortal.Models;
using CRMPortal.Services;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ErrorLogService _errorLogService;

        public AccountController(
            AppDbContext context,
            ErrorLogService errorLogService)
        {
            _context = context;
            _errorLogService = errorLogService;
        }


        // =========================================================
        // GET CLIENT IP ADDRESS
        // =========================================================
        private string GetClientIpAddress()
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress;

                if (remoteIp == null)
                {
                    return "Unknown";
                }

                // Convert IPv4-mapped IPv6 address
                // Example:
                // ::ffff:192.168.1.10
                // becomes:
                // 192.168.1.10
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    return remoteIp.MapToIPv4().ToString();
                }

                return remoteIp.ToString();
            }
            catch
            {
                return "Unknown";
            }
        }


        // =========================================================
        // LOGIN - GET
        // =========================================================
        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _ = _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(Login));

                ViewBag.Error =
                    "An unexpected error occurred while loading the login page.";

                return View();
            }
        }


        // =========================================================
        // LOGIN - POST
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                // =====================================================
                // MODEL VALIDATION
                // =====================================================
                if (!ModelState.IsValid)
                {
                    return View(model);
                }


                // =====================================================
                // FIND USER
                // =====================================================
                var user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Email == model.Email &&
                        x.Password == model.Password);


                // =====================================================
                // INVALID EMAIL / PASSWORD
                // =====================================================
                if (user == null)
                {
                    ViewBag.Error =
                        "Invalid Email or Password";

                    return View(model);
                }


                // =====================================================
                // GET CLIENT IP ADDRESS
                // =====================================================
                string ipAddress = GetClientIpAddress();


                // =====================================================
                // CHECK ACTIVE LOGIN FOR THIS PC
                // =====================================================
                var existingLogin = await _context.ActiveLogins
                    .FirstOrDefaultAsync(x =>
                        x.IpAddress == ipAddress);


                // =====================================================
                // ANOTHER USER IS ALREADY LOGGED IN
                // =====================================================
                if (existingLogin != null &&
                    existingLogin.UserId != user.UserId)
                {
                    ViewBag.Error =
                        "Another user is already logged in on this PC. Please sign out first.";

                    return View(model);
                }


                // =====================================================
                // SAME USER IS ALREADY LOGGED IN
                // =====================================================
                if (existingLogin != null &&
                    existingLogin.UserId == user.UserId)
                {
                    // Update last activity time
                    existingLogin.LastActivityTime = DateTime.Now;

                    await _context.SaveChangesAsync();
                }


                // =====================================================
                // NO ACTIVE LOGIN EXISTS
                // CREATE NEW SESSION + ACTIVE LOGIN
                // =====================================================
                else
                {
                    // =================================================
                    // CREATE SESSION
                    // =================================================
                    HttpContext.Session.SetInt32(
                        "UserId",
                        user.UserId);

                    HttpContext.Session.SetString(
                        "FullName",
                        user.FullName);

                    HttpContext.Session.SetInt32(
                        "RoleId",
                        user.RoleId);


                    // =================================================
                    // CREATE ACTIVE LOGIN
                    // =================================================
                    var activeLogin = new ActiveLogins
                    {
                        UserId = user.UserId,

                        IpAddress = ipAddress,

                        LoginTime = DateTime.Now,

                        LastActivityTime = DateTime.Now,

                        SessionId = HttpContext.Session.Id
                    };


                    _context.ActiveLogins.Add(activeLogin);

                    await _context.SaveChangesAsync();
                }


                // =====================================================
                // ADMIN
                // =====================================================
                if (user.RoleId == 1 || user.RoleId == 3)
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Admin");
                }


                // =====================================================
                // HR
                // =====================================================
                else if (user.RoleId == 5)
                {
                    return RedirectToAction(
                        "Dashboard",
                        "HR");
                }


                // =====================================================
                // EMPLOYEE
                // =====================================================
                else if (user.RoleId == 2)
                {
                    var employee =
                        await _context.MasterEmployee
                            .FirstOrDefaultAsync(x =>
                                x.Email == user.Email &&
                                !x.IsDeleted);


                    // =================================================
                    // EMPLOYEE PROFILE NOT FOUND
                    // =================================================
                    if (employee == null)
                    {
                        var loginToRemove =
                            await _context.ActiveLogins
                                .FirstOrDefaultAsync(x =>
                                    x.UserId == user.UserId &&
                                    x.IpAddress == ipAddress);


                        if (loginToRemove != null)
                        {
                            _context.ActiveLogins.Remove(loginToRemove);

                            await _context.SaveChangesAsync();
                        }


                        HttpContext.Session.Clear();


                        TempData["Error"] =
                            "Employee profile not found.";

                        return RedirectToAction("Login");
                    }


                    // =================================================
                    // SET EMPLOYEE SESSION
                    // =================================================
                    HttpContext.Session.SetInt32(
                        "EmployeeId",
                        employee.EmployeeId);

                    HttpContext.Session.SetString(
                        "EmployeeCode",
                        employee.EmployeeCode);

                    HttpContext.Session.SetString(
                        "EmployeeFullName",
                        employee.FullName);


                    return RedirectToAction(
                        "Dashboard",
                        "Employee");
                }


                // =====================================================
                // INVALID ROLE
                // =====================================================
                else
                {
                    var loginToRemove =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.UserId == user.UserId &&
                                x.IpAddress == ipAddress);


                    if (loginToRemove != null)
                    {
                        _context.ActiveLogins.Remove(loginToRemove);

                        await _context.SaveChangesAsync();
                    }


                    HttpContext.Session.Clear();


                    TempData["Error"] =
                        "Invalid Role.";

                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                // =====================================================
                // SAVE ERROR TO ERRORLOGS TABLE
                // =====================================================
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(Login));


                ViewBag.Error =
                    "An unexpected error occurred while logging in. Please try again.";

                return View(model);
            }
        }


        // =========================================================
        // LOGOUT
        // =========================================================
        public async Task<IActionResult> Logout()
        {
            try
            {
                // =====================================================
                // GET USER ID FROM SESSION
                // =====================================================
                int? userId =
                    HttpContext.Session.GetInt32("UserId");


                // =====================================================
                // GET CLIENT IP
                // =====================================================
                string ipAddress =
                    GetClientIpAddress();


                // =====================================================
                // REMOVE ACTIVE LOGIN
                // =====================================================
                if (userId.HasValue)
                {
                    var activeLogin =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.UserId == userId.Value &&
                                x.IpAddress == ipAddress);


                    if (activeLogin != null)
                    {
                        _context.ActiveLogins.Remove(activeLogin);

                        await _context.SaveChangesAsync();
                    }
                }


                // =====================================================
                // CLEAR SESSION
                // =====================================================
                HttpContext.Session.Clear();


                TempData["Success"] =
                    "Logged out successfully.";


                return RedirectToAction(
                    "Login",
                    "Account");
            }
            catch (Exception ex)
            {
                // =====================================================
                // SAVE ERROR TO ERRORLOGS TABLE
                // =====================================================
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(Logout));


                // =====================================================
                // CLEAR SESSION EVEN IF DATABASE ERROR OCCURS
                // =====================================================
                HttpContext.Session.Clear();


                TempData["Error"] =
                    "An error occurred while logging out.";


                return RedirectToAction(
                    "Login",
                    "Account");
            }
        }


        // =========================================================
        // FORGOT PASSWORD - GET
        // =========================================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _ = _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(ForgotPassword));

                ViewBag.Error =
                    "An unexpected error occurred while loading the page.";

                return View();
            }
        }


        // =========================================================
        // FORGOT PASSWORD - POST
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            try
            {
                // =====================================================
                // PASSWORD MATCH VALIDATION
                // =====================================================
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ViewBag.Error =
                        "Password and Confirm Password do not match.";

                    return View(model);
                }


                // =====================================================
                // FIND USER
                // =====================================================
                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(x =>
                            x.Email == model.Email);


                // =====================================================
                // USER NOT FOUND
                // =====================================================
                if (user == null)
                {
                    ViewBag.Error =
                        "Email does not exist.";

                    return View(model);
                }


                // =====================================================
                // UPDATE PASSWORD
                // =====================================================
                user.Password =
                    model.NewPassword;


                user.UpdatedDate =
                    DateTime.Now;


                await _context.SaveChangesAsync();


                // =====================================================
                // SUCCESS
                // =====================================================
                TempData["Success"] =
                    "Password changed successfully.";


                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // =====================================================
                // SAVE ERROR TO ERRORLOGS TABLE
                // =====================================================
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(ForgotPassword));


                ViewBag.Error =
                    "An unexpected error occurred while changing the password. Please try again.";

                return View(model);
            }
        }
    }
}

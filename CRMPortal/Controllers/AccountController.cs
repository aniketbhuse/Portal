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
        public async Task<IActionResult> Login()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
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
                // GET CLIENT IP
                // =====================================================
                string ipAddress = GetClientIpAddress();


                // =====================================================
                // EMPLOYEE PROFILE
                //
                // Only Employee requires MasterEmployee profile.
                // =====================================================
                MasterEmployee? employee = null;

                if (user.RoleId == 2)
                {
                    employee = await _context.MasterEmployee
                        .FirstOrDefaultAsync(x =>
                            x.Email == user.Email &&
                            !x.IsDeleted);

                    if (employee == null)
                    {
                        ViewBag.Error =
                            "Employee profile not found.";

                        return View(model);
                    }
                }


                // =====================================================
                // EMPLOYEE ACTIVE LOGIN RESTRICTIONS
                //
                // These restrictions apply ONLY to RoleId = 2.
                //
                // Employee rules:
                //
                // 1. Same employee cannot login on another PC/browser.
                //
                // 2. Same PC cannot be used by another employee/user
                //    while an employee session is active.
                //
                // Admin / Super Admin / HR are NOT restricted by
                // these ActiveLogin rules.
                // =====================================================

                if (user.RoleId == 2)
                {
                    // =================================================
                    // CHECK 1 - SAME EMPLOYEE ALREADY LOGGED IN
                    // =================================================

                    var existingUserLogin =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.UserId == user.UserId);


                    if (existingUserLogin != null)
                    {
                        ViewBag.Error =
                            $"{user.FullName} is already logged in. " +
                            "The same employee cannot login on another " +
                            "PC or browser until the current session is signed out.";

                        return View(model);
                    }


                    // =================================================
                    // CHECK 2 - SAME PC ALREADY HAS ACTIVE USER
                    // =================================================

                    var existingPcLogin =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.IpAddress == ipAddress);


                    if (existingPcLogin != null)
                    {
                        // =============================================
                        // FIND USER CURRENTLY USING THIS PC
                        // =============================================

                        var loggedInUser =
                            await _context.Users
                                .FirstOrDefaultAsync(x =>
                                    x.UserId ==
                                    existingPcLogin.UserId);


                        string loggedInUserName =
                            loggedInUser?.FullName ??
                            "Another user";


                        ViewBag.Error =
                            $"{loggedInUserName} is already logged in on this PC. " +
                            "Please logout from the current browser first.";

                        return View(model);
                    }
                }


                // =====================================================
                // CREATE ASP.NET SESSION
                // =====================================================

                HttpContext.Session.SetInt32(
                    "UserId",
                    user.UserId);

                HttpContext.Session.SetString(
                    "FullName",
                    user.FullName);

                HttpContext.Session.SetInt32(
                    "RoleId",
                    user.RoleId);


                // =====================================================
                // CREATE ACTIVE LOGIN
                //
                // IMPORTANT:
                // Only Employee needs ActiveLogins.
                //
                // Admin / Super Admin / HR will NOT create an
                // ActiveLogins restriction record.
                // =====================================================

                if (user.RoleId == 2)
                {
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
                // RoleId = 1
                // =====================================================

                if (user.RoleId == 1 || user.RoleId == 3)
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Admin");
                }


                // =====================================================
                // HR
                // RoleId = 5
                // =====================================================

                else if (user.RoleId == 5)
                {
                    return RedirectToAction(
                        "Dashboard",
                        "HR");
                }


                // =====================================================
                // EMPLOYEE
                // RoleId = 2
                // =====================================================

                else if (user.RoleId == 2)
                {
                    // Employee profile was already checked above.

                    HttpContext.Session.SetInt32(
                        "EmployeeId",
                        employee!.EmployeeId);

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
                    // Remove ActiveLogin only if one was created.
                    // Currently only Employee creates ActiveLogin,
                    // so this is mostly a safety check.

                    var loginToRemove =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.UserId == user.UserId &&
                                x.SessionId ==
                                HttpContext.Session.Id);


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
                // LOG ERROR
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
                // GET USER ID
                // =====================================================

                int? userId =
                    HttpContext.Session.GetInt32("UserId");


                // =====================================================
                // GET CURRENT SESSION ID
                // =====================================================

                string sessionId =
                    HttpContext.Session.Id;


                // =====================================================
                // REMOVE CURRENT ACTIVE LOGIN
                //
                // Only Employee will normally have an ActiveLogin.
                // Admin / Super Admin / HR simply won't find one.
                // =====================================================

                if (userId.HasValue)
                {
                    var activeLogin =
                        await _context.ActiveLogins
                            .FirstOrDefaultAsync(x =>
                                x.UserId == userId.Value &&
                                x.SessionId == sessionId);


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
                // LOG ERROR
                // =====================================================

                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(AccountController),
                    nameof(Logout));


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
        public async Task<IActionResult> ForgotPassword()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
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
                // LOG ERROR
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
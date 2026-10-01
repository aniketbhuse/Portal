using CRMPortal.Data;
using CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMPortal.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ErrorLogService _errorLogService;

        public ProfileController(
            AppDbContext context,
            ErrorLogService errorLogService)
        {
            _context = context;
            _errorLogService = errorLogService;
        }


        // =========================================================
        // PROFILE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // =================================================
                // GET LOGGED-IN USER ID
                // =================================================

                int? userId =
                    HttpContext.Session.GetInt32("UserId");


                // =================================================
                // CHECK LOGIN
                // =================================================

                if (!userId.HasValue)
                {
                    return RedirectToAction(
                        "Login",
                        "Account");
                }


                // =================================================
                // GET USER
                // =================================================

                var user = await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId.Value);


                if (user == null)
                {
                    HttpContext.Session.Clear();

                    return RedirectToAction(
                        "Login",
                        "Account");
                }


                // =================================================
                // SEND USER INFORMATION TO VIEW
                // =================================================

                ViewBag.User = user;


                // =================================================
                // EMPLOYEE PROFILE
                // =================================================

                if (user.RoleId == 2)
                {
                    var employee =
                        await _context.MasterEmployee
                            .FirstOrDefaultAsync(x =>
                                x.Email == user.Email &&
                                !x.IsDeleted);

                    ViewBag.Employee = employee;
                }


                // =================================================
                // ROLE NAME
                // =================================================

                string roleName = user.RoleId switch
                {
                    1 => "Admin",
                    2 => "Employee",
                    3 => "Super Admin",
                    5 => "HR",
                    _ => "User"
                };

                ViewBag.RoleName = roleName;


                return View();
            }
            catch (Exception ex)
            {
                await _errorLogService.LogErrorAsync(
                    ex,
                    nameof(ProfileController),
                    nameof(Index));

                TempData["Error"] =
                    "Unable to load your profile.";

                return RedirectToAction(
                    "Dashboard",
                    "Admin");
            }
        }
    }
}
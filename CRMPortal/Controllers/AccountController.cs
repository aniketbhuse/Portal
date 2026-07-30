using CRMPortal.Data;
using CRMPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRMPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.Users
                .FirstOrDefault(x =>
                    x.Email == model.Email &&
                    x.Password == model.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid Email or Password";
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);

            HttpContext.Session.SetString("FullName", user.FullName);

            HttpContext.Session.SetInt32("RoleId", user.RoleId);

            if (user.RoleId == 1 || user.RoleId == 3)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (user.RoleId == 5)
            {
                return RedirectToAction("Dashboard", "HR");
            }
            else if (user.RoleId == 2)
            {
                return RedirectToAction("Dashboard", "Employee");
            }
            else
            {
                TempData["Error"] = "Invalid Role.";

                return RedirectToAction("Login");
            }

        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Password and Confirm Password do not match.";
                return View(model);
            }

            var user = _context.Users
                .FirstOrDefault(x => x.Email == model.Email);

            if (user == null)
            {
                ViewBag.Error = "Email does not exist.";
                return View(model);
            }

            user.Password = model.NewPassword;

            // Update UpdatedDate
            user.UpdatedDate = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Password changed successfully.";

            return RedirectToAction("Login");
        }
    }
}
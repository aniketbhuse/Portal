using CRMPortal.Data;
using Microsoft.AspNetCore.Mvc;

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
                if (HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                return View();
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
                if(HttpContext.Session.GetInt32("RoleId") != 5)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employees = _context.MasterEmployee
                                .Where(x => x.IsDeleted == false)
                                .OrderBy(x => x.EmployeeCode)
                                .ToList();

                return View(employees);
            }
            catch(Exception ex)
            {
                TempData["Error"] = ex.Message;

                return RedirectToAction("Dashboard");
            }

        }
    }
}

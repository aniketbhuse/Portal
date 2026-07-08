using CRMPortal.Data;
using CRMPortal.Models;
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
                if (HttpContext.Session.GetInt32("RoleId") != 4)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Total Employees
                ViewBag.TotalEmployees =
                    _context.MasterEmployee
                            .Count(x => x.IsDeleted == false);

                // Active Employees
                ViewBag.ActiveEmployees =
                    _context.MasterEmployee
                            .Count(x => x.EmployeeStatus == "Active"
                                     && x.IsDeleted == false);

                // Inactive Employees
                ViewBag.InactiveEmployees =
                    _context.MasterEmployee
                            .Count(x => x.EmployeeStatus == "Inactive"
                                     && x.IsDeleted == false);

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
                if(HttpContext.Session.GetInt32("RoleId") != 4)
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

        public IActionResult DeleteEmployee(int id)
        {
            try
            {
                var employee = _context.MasterEmployee
                                       .FirstOrDefault(x => x.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";

                    return RedirectToAction("Employees");
                }

                employee.IsDeleted = true;
                employee.ModifiedDate = DateTime.Now;

                _context.SaveChanges();

                TempData["Success"] = "Employee deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Employees");
        }

        [HttpPost]
        public IActionResult AddEmployee(MasterEmployee model)
        {
            try
            {
                if (HttpContext.Session.GetInt32("RoleId") != 4)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!ModelState.IsValid)
                {
                    return View("Dashboard");
                }

                model.CreatedDate = DateTime.Now;

                model.UpdatedDate = DateTime.Now;

                model.ModifiedDate = DateTime.Now;

                model.IsDeleted = false;

                _context.MasterEmployee.Add(model);

                _context.SaveChanges();

                TempData["Success"] = "Employee Added Successfully.";

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

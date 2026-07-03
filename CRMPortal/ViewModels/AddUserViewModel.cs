using CRMPortal.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRMPortal.ViewModels
{
    public class AddUserViewModel
    {
        public Users Users { get; set; } = new Users();

        public List<SelectListItem> Roles { get; set; }
            = new List<SelectListItem>();

        public List<Users> AdminUsers { get; set; }
            = new List<Users>();

        public List<Users> EmployeeUsers { get; set; }
            = new List<Users>();
    }
}
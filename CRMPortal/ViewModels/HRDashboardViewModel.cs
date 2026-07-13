using CRMPortal.Models;
using System.Collections.Generic;

namespace CRMPortal.ViewModels
{
    public class HRDashboardViewModel
    {
        public MasterEmployee MasterEmployee { get; set; }

        public EmployeeAttendance EmployeeAttendance { get; set; }

        public List<MasterEmployee> EmployeeList { get; set; }

        public int TotalEmployees { get; set; }

        public int ActiveEmployees { get; set; }

        public int InactiveEmployees { get; set; }
    }
}
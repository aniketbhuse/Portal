using CRMPortal.Models;

namespace CRMPortal.ViewModels
{
    public class LoginTrackerViewModel
    {
        public int EmployeeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public List<MasterEmployee> EmployeeList { get; set; }

        public List<EmployeeLoginTracker> LoginList { get; set; }

        // Summary Cards

        public int TotalPresentDays { get; set; }

        public decimal TotalWorkingHours { get; set; }

        public int LateLoginCount { get; set; }

        public int NotSignedOutCount { get; set; }

        public decimal AverageWorkingHours { get; set; }
    }
}
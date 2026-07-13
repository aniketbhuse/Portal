using CRMPortal.Models;

namespace CRMPortal.ViewModels
{
    public class AttendanceReportViewModel
    {
        public int EmployeeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public List<MasterEmployee> EmployeeList { get; set; }

        public List<EmployeeAttendance> AttendanceList { get; set; }

        public int TotalDays { get; set; }

        public int PresentDays { get; set; }

        public int AbsentDays { get; set; }

        public double AttendancePercentage { get; set; }


        // salary deducation part 

        public decimal MonthlySalary { get; set; }

        public decimal PerDaySalary { get; set; }

        public decimal SalaryDeduction { get; set; }

        public decimal NetSalary { get; set; }

        public int WorkingDays { get; set; }

        public int SaturdayCount { get; set; }

        public int SundayCount { get; set; }
    }
}
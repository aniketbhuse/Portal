namespace CRMPortal.ViewModels
{
    public class EmployeeAttendanceSummaryViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public decimal MonthlySalary { get; set; }

        public decimal PerDaySalary { get; set; }

        public int PresentDays { get; set; }

        public int PaidLeaveCount { get; set; }

        public int WorkFromHomeCount { get; set; }

        public int HalfDayCount { get; set; }

        public int EmergencyLeaveCount { get; set; }

        public int LWPCount { get; set; }

        public int ULWPCount { get; set; }

        public int SandwichCount { get; set; }

        public int NCNSCount { get; set; }

        public int OfficialHolidayCount { get; set; }

        public int HolidayCount { get; set; }

        public int AbscondCount { get; set; }

        public int TerminatedCount { get; set; }

        public decimal AttendanceDays { get; set; }

        public double AttendancePercentage { get; set; }

        public decimal HalfDayDeduction { get; set; }

        public decimal LWPDeduction { get; set; }

        public decimal ULWPDeduction { get; set; }

        public decimal SandwichDeduction { get; set; }

        public decimal NCNSDeduction { get; set; }

        public decimal AbscondDeduction { get; set; }

        public decimal TerminatedDeduction { get; set; }

        public decimal SalaryDeduction { get; set; }

        public decimal NetSalary { get; set; }
    }
}

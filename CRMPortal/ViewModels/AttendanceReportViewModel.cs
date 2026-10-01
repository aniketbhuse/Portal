
using CRMPortal.Models;

namespace CRMPortal.ViewModels
{
    public class AttendanceReportViewModel
    {
        // ==========================================
        // FILTERS
        // ==========================================
        public int EmployeeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }


        // ==========================================
        // EMPLOYEE DATA
        // ==========================================
        public List<MasterEmployee> EmployeeList { get; set; } = new();

        public List<EmployeeAttendance> AttendanceList { get; set; } = new();


        // ==========================================
        // ATTENDANCE SUMMARY
        // ==========================================
        public int TotalDays { get; set; }

        public int WorkingDays { get; set; }

        public int SaturdayCount { get; set; }

        public int SundayCount { get; set; }

        // Saturday + Sunday
        public int HolidayCount { get; set; }

        // Official Holiday marked as H
        public int OfficialHolidayCount { get; set; }

        public int PresentDays { get; set; }

        public int AbsentDays { get; set; }


        // ==========================================
        // LEAVE / ATTENDANCE SUMMARY
        // ==========================================
        public int PaidLeaveCount { get; set; }

        public int WorkFromHomeCount { get; set; }

        public int HalfDayCount { get; set; }

        public int EmergencyLeaveCount { get; set; }

        public int LWPCount { get; set; }

        public int ULWPCount { get; set; }

        // Sandwich Leave
        public int SandwichCount { get; set; }

        // No Call No Show
        public int NCNSCount { get; set; }

        public int AbscondCount { get; set; }

        public int TerminatedCount { get; set; }


        // ==========================================
        // ATTENDANCE CALCULATION
        // ==========================================
        public decimal AttendanceDays { get; set; }

        public double AttendancePercentage { get; set; }


        // ==========================================
        // SALARY CALCULATION
        // ==========================================
        public decimal MonthlySalary { get; set; }

        public decimal PerDaySalary { get; set; }

        public decimal SalaryDeduction { get; set; }

        public decimal NetSalary { get; set; }


        // ==========================================
        // DEDUCTION BREAKDOWN
        // ==========================================
        public decimal HalfDayDeduction { get; set; }

        public decimal LWPDeduction { get; set; }

        public decimal ULWPDeduction { get; set; }

        // 3 days deduction
        public decimal SandwichDeduction { get; set; }

        // 3 days deduction
        public decimal NCNSDeduction { get; set; }

        // Reserved for reporting if required
        public decimal AbscondDeduction { get; set; }

        // Reserved for reporting if required
        public decimal TerminatedDeduction { get; set; }


        // ==========================================
        // PAID LEAVE BALANCE
        // ==========================================
        public decimal PaidLeaveBalance { get; set; }

        public List<EmployeeAttendanceSummaryViewModel> EmployeeSummaryList { get; set; } = new();
    }
}


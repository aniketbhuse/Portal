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
        public int TotalDays { get; set; }          // 30 / 31 / 28 / 29

        public int WorkingDays { get; set; }        // Total Days - Holidays

        public int SaturdayCount { get; set; }

        public int SundayCount { get; set; }

        public int HolidayCount { get; set; }

        public int PresentDays { get; set; }

        public int AbsentDays { get; set; }

        // ==========================================
        // LEAVE SUMMARY
        // ==========================================
        public int PaidLeaveCount { get; set; }         // PL

        public int WorkFromHomeCount { get; set; }      // WFH

        public int HalfDayCount { get; set; }           // HD

        public int EmergencyLeaveCount { get; set; }    // EL

        public int LWPCount { get; set; }               // Leave Without Pay

        public int ULWPCount { get; set; }              // Unplanned Leave Without Pay

        public int SandwichCount { get; set; }          // Sandwich Leave

        public int AbscondCount { get; set; }           // Abscond

        public int TerminatedCount { get; set; }        // Terminated

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

        public decimal SandwichDeduction { get; set; }

        public decimal AbscondDeduction { get; set; }

        public decimal TerminatedDeduction { get; set; }

        // ==========================================
        // PAID LEAVE BALANCE
        // ==========================================
        public decimal PaidLeaveBalance { get; set; }
    }
}
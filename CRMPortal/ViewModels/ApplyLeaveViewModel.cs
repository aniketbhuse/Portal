using CRMPortal.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CRMPortal.ViewModels
{
    public class ApplyLeaveViewModel
    {
        // ==========================================
        // LEAVE APPLY FORM
        // ==========================================
        public int LeaveTypeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public string Reason { get; set; }

        // ==========================================
        // LEAVE TYPE DROPDOWN
        // ==========================================
        public List<SelectListItem> LeaveTypes { get; set; } = new();

        // ==========================================
        // MY LEAVE REQUESTS TABLE
        // ==========================================
        public List<LeaveRequests> MyLeaves { get; set; } = new();

        // ==========================================
        // LEAVE BALANCE CARDS
        // ==========================================
        public decimal MonthlyLeaveEarned { get; set; }

        public decimal AnnualLeaveBalance { get; set; }

        public decimal LeaveUsedThisYear { get; set; }

        public decimal RemainingLeaveBalance { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class LeaveRequests
    {
        [Key]
        public int LeaveRequestId { get; set; }

        public int UserId { get; set; }

        public int LeaveTypeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public string Reason { get; set; }

        public string Status { get; set; }

        public string? AdminRemarks { get; set; }

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }
    }
}
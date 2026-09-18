using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class EmployeeLoginTracker
    {
        [Key]
        public int LoginTrackerId { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string FullName { get; set; }

        public DateOnly LoginDate { get; set; }

        public DateTime? SignInTime { get; set; }

        public DateTime? SignOutTime { get; set; }

        public decimal? TotalWorkingHours { get; set; }

        public string SessionStatus { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}

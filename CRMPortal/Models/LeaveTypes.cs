using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class LeaveTypes
    {
        [Key]
        public int LeaveTypeId { get; set; }

        public string LeaveTypeName { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }
    }
}

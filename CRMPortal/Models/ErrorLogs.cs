using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMPortal.Models
{
    public partial class ErrorLogs
    {
        [Key]
        public int ErrorLogId { get; set; }

        [StringLength(100)]
        public string? ControllerName { get; set; }

        [StringLength(100)]
        public string? ActionName { get; set; }

        public string? ErrorMessage { get; set; }

        public string? StackTrace { get; set; }

        public string? InnerException { get; set; }

        public int? UserId { get; set; }

        [StringLength(200)]
        public string? UserName { get; set; }

        [StringLength(100)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? Browser { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; }
    }
}
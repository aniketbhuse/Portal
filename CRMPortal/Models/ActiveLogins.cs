using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class ActiveLogins
    {
        [Key]
        public int ActiveLoginId { get; set; }

        public int UserId { get; set; }

        public string IpAddress { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; }

        public DateTime LastActivityTime { get; set; }

        public string? SessionId { get; set; }
    }
}
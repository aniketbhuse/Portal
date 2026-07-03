using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class EmployeeFiles
    {
        [Key]
        public int FileId { get; set; }

        public int UserId { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public long? FileSize { get; set; }

        public DateTime UploadedDate { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace CRMPortal.Models
{
    public class MasterEmployee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        public string EmployeeCode { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Gender { get; set; }

        public DateOnly DateOfBirth { get; set; }

        [Required]
        public string BloodGroup { get; set; }

        [Required]
        public string MaritalStatus { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string MobileNumber { get; set; }

        public string? AlternateMobile { get; set; }

        [Required]
        public string EmergencyContactName { get; set; }

        [Required]
        public string EmergencyContact { get; set; }

        [Required]
        public string AddressLine1 { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string Pincode { get; set; }

        [Required]
        public string Department { get; set; }

        [Required]
        public string Designation { get; set; }

        public DateOnly JoiningDate { get; set; }

        [Required]
        public string EmploymentType { get; set; }

        public string? ReportingManager { get; set; }

        [Required]
        public string WorkLocation { get; set; }

        [Required]
        public string Shift { get; set; }

        public decimal Salary { get; set; }

        public string? AadhaarNumber { get; set; }

        public string? PANNumber { get; set; }

        public string? PassportNumber { get; set; }

        public string? BankName { get; set; }

        public string? AccountNumber { get; set; }

        public string? IFSCCode { get; set; }

        [Required]
        public string EmployeeStatus { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public DateTime ModifiedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}
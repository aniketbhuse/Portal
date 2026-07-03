namespace CRMPortal.ViewModels
{
    public class ApplyLeaveViewModel
    {
        public int LeaveTypeId { get; set; }

        public DateOnly FromDate { get; set; }

        public DateOnly ToDate { get; set; }

        public string Reason { get; set; }
    }
}
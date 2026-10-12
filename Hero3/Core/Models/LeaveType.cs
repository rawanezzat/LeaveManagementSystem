namespace Hero.Models
{
    public class LeaveType
    {
        public int LeaveTypeId { get; set; }
        public string TypeName { get; set; } = null!;
        public int? MaxDaysPerYear { get; set; }
        public int? MaxConsecutiveDays { get; set; }
        public int? MinNoticeDays { get; set; }
        public int? MaxHoursPerRequest { get; set; }
        public int? MaxTimesPerMonth { get; set; }
        public bool RequiresApproval { get; set; }
        public bool RequiresMedicalNote { get; set; }
        public bool RequiresAttachment { get; set; }
        public bool MustUsePaidLeaveFirst { get; set; }
        public bool IsUnpaid { get; set; }
        public string? Notes { get; set; }


        //Request links
        public ICollection<LeaveRequest> leaveRequests { get; set; } = new List<LeaveRequest>();


    }
}

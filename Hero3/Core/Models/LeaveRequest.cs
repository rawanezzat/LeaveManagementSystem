using Hero3.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Hero.Models
{
    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }
        public DateTime RequestStartDate { get; set; }
        public DateTime RequestEndDate { get; set; }
        public string RequestStatus { get; set; } = "Pending";
        public TimeSpan? PersonalStartTime { get; set; } // For Personal leave only
        public TimeSpan? PersonalEndTime { get; set; }   // For Personal leave only

        public string? Reason { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? MedicalCertificateIssuer { get; set; }  
        public string? AttachmentPath { get; set; }  
        public DateTime? NotifiedDate { get; set; }

        public bool IsDeleted { get; set; } = false;

        //user links
        public string? UserId { get; set; }   // Foreign key
        public ApplicationUser? ApplicationUser { get; set; }


        //Requesttype links
        [Required(ErrorMessage = "Please select a leave type")]
        public int? LeaveTypeId { get; set; }  // <-- nullable here
        public LeaveType? LeaveType { get; set; }




    }
}

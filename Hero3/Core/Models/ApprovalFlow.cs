using Azure.Core;
using Hero3.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Hero.Models
{
    public class ApprovalFlow
    {
        public int ApprovalFlowId { get; set; }   
        public int ApprovalOrder { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime? ActionDate { get; set; }

        public string? Remarks { get; set; }


        // Link  leave request with approvalflow
        public int LeaveRequestId { get; set; }
        public LeaveRequest leaveRequest { get; set; }


        // link user with 
        public int Id { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}

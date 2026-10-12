using Hero.Models;
using Microsoft.AspNetCore.Identity;


namespace Hero3.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string UserPosition { get; set; } = null!;
        public string UserDepartment { get; set; } = null!;
        public DateTime HireDate { get; set; }
        public int AnnualLeaveBalance { get; set; } = 21;
        public bool IsDeleted { get; set; } = false;

        public string? LineManagerId { get; set; }  
        public ApplicationUser? LineManager { get; set; }

        public ICollection<LeaveRequest> leaveRequests { get; set; } = new List<LeaveRequest>();
    }

}


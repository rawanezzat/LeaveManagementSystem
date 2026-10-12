using Hero.Models;
using Hero3.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;

namespace Hero3.Controllers
{
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MailSettings _mailSettings;

        public LeaveRequestsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            IOptions<MailSettings> mailSettings)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _mailSettings = mailSettings.Value;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _context.LeaveRequests.Where(lr => !lr.IsDeleted);

            if (User.IsInRole("Employee") && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(lr => lr.UserId == userId);
            }

            ViewBag.PendingCount = query.Count(lr => lr.RequestStatus == "Pending");
            ViewBag.RejectedCount = query.Count(lr => lr.RequestStatus == "Rejected");
            ViewBag.ApprovedCount = query.Count(lr => lr.RequestStatus == "Approved");
            ViewBag.TotalCount = query.Count();

            if (!string.IsNullOrEmpty(userId))
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                ViewBag.AnnualLeaveBalance = user?.AnnualLeaveBalance ?? 0;
            }
            else
            {
                ViewBag.AnnualLeaveBalance = 0;
            }
        }

        // GET: Index
        public async Task<IActionResult> Index(string viewAll = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            var query = _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .Include(l => l.LeaveType)
                .Where(l => !l.IsDeleted);

            if (!string.IsNullOrEmpty(viewAll))
            {
                if (roles.Contains("Employee"))
                {
                    query = query.Where(l => l.UserId == userId);
                }
                else if (roles.Contains("HR") || roles.Contains("Manager") || roles.Contains("Admin"))
                {
                    // Show all requests for admins when viewAll is true
                }
            }
            else
            {
                if (roles.Contains("Employee"))
                {
                    query = query.Where(l => l.UserId == userId && l.RequestStatus == "Pending");
                }
            else if (roles.Contains("HR") || roles.Contains("Manager") || roles.Contains("Admin"))
            {
                query = query.Where(l => l.RequestStatus == "Pending");
            }
            }

            var leaveRequests = await query.ToListAsync();
            return View(leaveRequests);
        }


        // GET: Create
        public IActionResult Create()
        {
            ViewBag.LeaveTypesSelectList = new SelectList(_context.LeaveTypes, "LeaveTypeId", "TypeName");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNew(LeaveRequest leaveRequest, IFormFile? attachmentFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            leaveRequest.UserId = user.Id; // correct FK

            int requestedDays = (leaveRequest.RequestEndDate - leaveRequest.RequestStartDate).Days + 1;

            if (leaveRequest.LeaveTypeId != 11) // not unpaid
            {
                if (user.AnnualLeaveBalance <= 0)
                {
                    ModelState.AddModelError("", "You have no remaining paid leave days.");
                }
                else if (requestedDays > user.AnnualLeaveBalance)
                {
                    ModelState.AddModelError("", $"You only have {user.AnnualLeaveBalance} days remaining.");
                }
            }

            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(attachmentFile.FileName);
                using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await attachmentFile.CopyToAsync(stream);
                leaveRequest.AttachmentPath = "/uploads/" + fileName;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LeaveTypesSelectList = new SelectList(_context.LeaveTypes, "LeaveTypeId", "TypeName", leaveRequest.LeaveTypeId);
                return View("Create", leaveRequest);
            }

            leaveRequest.RequestStatus = "Pending";
            leaveRequest.CreatedDate = DateTime.Now;

            await _context.LeaveRequests.AddAsync(leaveRequest);
            await _context.SaveChangesAsync();

            // ✅ Send email to Line Manager
            if (!string.IsNullOrEmpty(user.LineManagerId))
            {
                var lineManager = await _userManager.FindByIdAsync(user.LineManagerId);

                if (lineManager != null && !string.IsNullOrEmpty(lineManager.Email))
                {
                    try
                    {
                        var mail = new MailMessage
                        {
                            From = new MailAddress(_mailSettings.Email, _mailSettings.DisplayName),
                            Subject = "New Leave Request Submitted",
                            Body = $"Hello {lineManager.UserName},\n\n" +
                                   $"Employee {user.UserName} has submitted a new leave request.\n" +
                                   $"From: {leaveRequest.RequestStartDate:yyyy-MM-dd}\n" +
                                   $"To: {leaveRequest.RequestEndDate:yyyy-MM-dd}\n" +
                                   $"Reason: {leaveRequest.Reason}\n\n" +
                                   $"Please review it in the system.",
                            IsBodyHtml = false
                        };

                        mail.To.Add(lineManager.Email);

                        using var smtp = new SmtpClient("smtp.gmail.com", _mailSettings.Port)
                        {
                            Credentials = new NetworkCredential(_mailSettings.Email, _mailSettings.Password),
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false
                        };

                        await smtp.SendMailAsync(mail);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("❌ Error sending email: " + ex.Message);
                    }
                }
            }

            TempData["SuccessMessage"] = "Leave request added successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveRequest leaveRequest, IFormFile? attachmentFile)
        {
            if (leaveRequest.LeaveRequestId == 0) return NotFound();

            var existingRequest = await _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == leaveRequest.LeaveRequestId);

            if (existingRequest == null) return NotFound();

            var user = await _userManager.FindByIdAsync(existingRequest.UserId);
            if (user == null) return Unauthorized();

            int requestedDays = (leaveRequest.RequestEndDate - leaveRequest.RequestStartDate).Days + 1;

            if (leaveRequest.LeaveTypeId != 11) 
            {
                if (user.AnnualLeaveBalance <= 0)
                {
                    ModelState.AddModelError("", "You have no remaining paid leave days.");
                }
                else if (requestedDays > user.AnnualLeaveBalance)
                {
                    ModelState.AddModelError("", $"You only have {user.AnnualLeaveBalance} days remaining.");
                }
            }

            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(attachmentFile.FileName);
                using var stream = new FileStream(Path.Combine(uploadsFolder, fileName), FileMode.Create);
                await attachmentFile.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(existingRequest.AttachmentPath))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingRequest.AttachmentPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                existingRequest.AttachmentPath = "/uploads/" + fileName;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LeaveTypesSelectList = new SelectList(_context.LeaveTypes, "LeaveTypeId", "TypeName", leaveRequest.LeaveTypeId);
                return View(leaveRequest); 
                
            }

            existingRequest.RequestStartDate = leaveRequest.RequestStartDate;
            existingRequest.RequestEndDate = leaveRequest.RequestEndDate;
            existingRequest.Reason = leaveRequest.Reason;
            existingRequest.LeaveTypeId = leaveRequest.LeaveTypeId;
            existingRequest.MedicalCertificateIssuer = leaveRequest.MedicalCertificateIssuer;
            existingRequest.RequestStatus = "Pending";


            _context.Update(existingRequest);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request updated successfully!";
            return RedirectToAction("Index");
        }

        // Edit GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .AsNoTracking() // don’t track, we’re just displaying
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null) return NotFound();

            ViewBag.LeaveTypesSelectList = new SelectList(
                await _context.LeaveTypes.ToListAsync(),
                "LeaveTypeId",
                "TypeName",
                leaveRequest.LeaveTypeId // 👈 this will now have the correct value
            );

            return View(leaveRequest); // 👈 return full view instead of PartialView
        }

        // Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .Include(l => l.LeaveType)
                .FirstOrDefaultAsync(m => m.LeaveRequestId == id);

            if (leaveRequest == null) return NotFound();

            return PartialView("_EditPartial", leaveRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCurrent(int leaveRequestId)
        {
            var leaveRequest = _context.LeaveRequests.FirstOrDefault(l => l.LeaveRequestId == leaveRequestId);
            if (leaveRequest == null) return NotFound();

            leaveRequest.IsDeleted = true;
            _context.Update(leaveRequest);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // Filters
        public async Task<IActionResult> Pending()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.IsInRole("Employee") ? "Employee" : "Other";

            var query = _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .Include(l => l.LeaveType)
                .Where(l => !l.IsDeleted && l.RequestStatus == "Pending");

            if (userRole == "Employee") query = query.Where(l => l.UserId == userId);

            var leaveRequests = await query.ToListAsync();
            return View("Index", leaveRequests);
        }

        public async Task<IActionResult> Approved()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .Include(l => l.LeaveType)
                .Where(l => !l.IsDeleted && l.RequestStatus == "Approved");

            if (User.IsInRole("Employee"))
            {
                query = query.Where(l => l.UserId == userId);
            }

            return View("Index", await query.ToListAsync());
        }

        public async Task<IActionResult> Rejected()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .Include(l => l.LeaveType)
                .Where(l => !l.IsDeleted && l.RequestStatus == "Rejected");

            if (User.IsInRole("Employee"))
            {
                query = query.Where(l => l.UserId == userId);
            }

            return View("Index", await query.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            Console.WriteLine($"🚀 ApproveRequest called for ID: {id}");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.RequestStatus != "Approved")
            {
                leaveRequest.RequestStatus = "Approved";

                int leaveDays = (leaveRequest.RequestEndDate - leaveRequest.RequestStartDate).Days + 1;
                leaveRequest.ApplicationUser.AnnualLeaveBalance -= leaveDays;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Leave request {id} approved by {currentUser?.UserName}. Sending email to: {leaveRequest.ApplicationUser.Email}");

                // Send email to employee about approval
                Console.WriteLine($"🔍 Checking email for user {leaveRequest.ApplicationUser.UserName}: {leaveRequest.ApplicationUser.Email}");
                Console.WriteLine($"📧 Current user (approver): {currentUser?.UserName} - {currentUser?.Email}");
                if (!string.IsNullOrEmpty(leaveRequest.ApplicationUser.Email))
                {
                    Console.WriteLine($"📤 Attempting to send approval email to: {leaveRequest.ApplicationUser.Email}");
                    try
                    {
                        var mail = new MailMessage
                        {
                            From = new MailAddress(_mailSettings.Email, _mailSettings.DisplayName),
                            Subject = "Leave Request Approved - Hero System",
                            Body = $"Dear {leaveRequest.ApplicationUser.UserName},\n\n" +
                                   $"Good news! Your leave request has been APPROVED.\n\n" +
                                   $"📅 Leave Details:\n" +
                                   $"   From: {leaveRequest.RequestStartDate:yyyy-MM-dd}\n" +
                                   $"   To: {leaveRequest.RequestEndDate:yyyy-MM-dd}\n" +
                                   $"   Duration: {leaveDays} day(s)\n" +
                                   $"   Type: {leaveRequest.LeaveType?.TypeName}\n" +
                                   $"   Reason: {leaveRequest.Reason}\n\n" +
                                   $"✅ Approved by: {currentUser?.UserPosition} - {currentUser?.UserName}\n\n" +
                                   $"Your leave balance has been updated accordingly. Enjoy your time off!\n\n" +
                                   $"Best regards,\n" +
                                   $"Hero Leave Management System\n" +
                                   $"{_mailSettings.DisplayName}",
                            IsBodyHtml = false
                        };

                        mail.To.Add(leaveRequest.ApplicationUser.Email);
                        Console.WriteLine($"📧 Mail configured - From: {_mailSettings.Email}, To: {leaveRequest.ApplicationUser.Email}");

                        using var smtp = new SmtpClient("smtp.gmail.com", _mailSettings.Port)
                        {
                            Credentials = new NetworkCredential(_mailSettings.Email, _mailSettings.Password),
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false
                        };

                        Console.WriteLine("📨 Sending email...");
                        await smtp.SendMailAsync(mail);
                        Console.WriteLine($"✅ Approval email sent successfully to {leaveRequest.ApplicationUser.Email}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error sending approval email: {ex.Message}");
                        Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int id)
        {
            Console.WriteLine($"🚫 RejectRequest called for ID: {id}");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.ApplicationUser)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            if (leaveRequest.RequestStatus != "Rejected")
            {
                leaveRequest.RequestStatus = "Rejected";

                int leaveDays = (leaveRequest.RequestEndDate - leaveRequest.RequestStartDate).Days + 1;
                leaveRequest.ApplicationUser.AnnualLeaveBalance += leaveDays;

                await _context.SaveChangesAsync();
                Console.WriteLine($"❌ Leave request {id} rejected by {currentUser?.UserName}. Sending email to: {leaveRequest.ApplicationUser.Email}");

                // Send email to employee about rejection
                Console.WriteLine($"🔍 Checking email for user {leaveRequest.ApplicationUser.UserName}: {leaveRequest.ApplicationUser.Email}");
                Console.WriteLine($"📧 Current user (rejector): {currentUser?.UserName} - {currentUser?.Email}");
                if (!string.IsNullOrEmpty(leaveRequest.ApplicationUser.Email))
                {
                    Console.WriteLine($"📤 Attempting to send rejection email to: {leaveRequest.ApplicationUser.Email}");
                    try
                    {
                        var mail = new MailMessage
                        {
                            From = new MailAddress(_mailSettings.Email, _mailSettings.DisplayName),
                            Subject = "Leave Request Rejected - Hero System",
                            Body = $"Dear {leaveRequest.ApplicationUser.UserName},\n\n" +
                                   $"We regret to inform you that your leave request has been REJECTED.\n\n" +
                                   $"📅 Leave Details:\n" +
                                   $"   From: {leaveRequest.RequestStartDate:yyyy-MM-dd}\n" +
                                   $"   To: {leaveRequest.RequestEndDate:yyyy-MM-dd}\n" +
                                   $"   Duration: {leaveDays} day(s)\n" +
                                   $"   Type: {leaveRequest.LeaveType?.TypeName}\n" +
                                   $"   Reason: {leaveRequest.Reason}\n\n" +
                                   $"❌ Rejected by: {currentUser?.UserPosition} - {currentUser?.UserName}\n\n" +
                                   $"Please contact your manager for more details or to discuss alternative arrangements.\n\n" +
                                   $"Best regards,\n" +
                                   $"Hero Leave Management System\n" +
                                   $"{_mailSettings.DisplayName}",
                            IsBodyHtml = false
                        };

                        mail.To.Add(leaveRequest.ApplicationUser.Email);
                        Console.WriteLine($"📧 Mail configured - From: {_mailSettings.Email}, To: {leaveRequest.ApplicationUser.Email}");

                        using var smtp = new SmtpClient("smtp.gmail.com", _mailSettings.Port)
                        {
                            Credentials = new NetworkCredential(_mailSettings.Email, _mailSettings.Password),
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false
                        };

                        Console.WriteLine("📨 Sending email...");
                        await smtp.SendMailAsync(mail);
                        Console.WriteLine($"✅ Rejection email sent successfully to {leaveRequest.ApplicationUser.Email}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error sending rejection email: {ex.Message}");
                        Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                    }
                }
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> Search(DateTime? RequestStartDate, DateTime? RequestEndDate,
                                string? RequestStatus, string? Reason,
                                string? MedicalCertificateIssuer, string? UserName)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isEmployee = User.IsInRole("Employee");
            var isManager = User.IsInRole("Manager") || User.IsInRole("HR") || User.IsInRole("Admin");

            IQueryable<LeaveRequest> query = _context.LeaveRequests
                .Include(l => l.ApplicationUser);

            if (isEmployee && !isManager)
            {
                query = query.Where(l => l.UserId == userId);
            }

            if (RequestStartDate.HasValue)
                query = query.Where(l => l.RequestStartDate >= RequestStartDate.Value);

            if (RequestEndDate.HasValue)
                query = query.Where(l => l.RequestEndDate <= RequestEndDate.Value);

            if (!string.IsNullOrEmpty(RequestStatus))
                query = query.Where(l => l.RequestStatus.Contains(RequestStatus));

            if (!string.IsNullOrEmpty(Reason))
                query = query.Where(l => l.Reason.Contains(Reason));

            if (!string.IsNullOrEmpty(MedicalCertificateIssuer))
                query = query.Where(l => l.MedicalCertificateIssuer.Contains(MedicalCertificateIssuer));

            if (!string.IsNullOrEmpty(UserName) && isManager)
                query = query.Where(l => l.ApplicationUser.UserName.Contains(UserName));

            var results = await query.ToListAsync();

            return View("SearchResults", results);
        }

        // Action to reset leave balance for current user (for testing purposes)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,HR")]
        public async Task<IActionResult> ResetLeaveBalance(string userId = null)
        {
            try
            {
                // If no userId provided, reset current user's balance
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Reset to default balance (21 days)
                user.AnnualLeaveBalance = 21;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"Leave balance for {user.UserName} has been reset to 21 days.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error resetting leave balance: " + ex.Message;
                return RedirectToAction("Index");
            }
        }



    }
}

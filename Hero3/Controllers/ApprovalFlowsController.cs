//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using Hero.Models;

//namespace Hero3.Controllers
//{
//    public class ApprovalFlowsController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public ApprovalFlowsController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // GET: ApprovalFlows
//        public async Task<IActionResult> Index()
//        {
//            var applicationDbContext = _context.ApprovalFlows.Include(a => a.ApplicationUser).Include(a => a.leaveRequest);
//            return View(await applicationDbContext.ToListAsync());
//        }

//        // GET: ApprovalFlows/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var approvalFlow = await _context.ApprovalFlows
//                .Include(a => a.ApplicationUser)
//                .Include(a => a.leaveRequest)
//                .FirstOrDefaultAsync(m => m.ApprovalFlowId == id);
//            if (approvalFlow == null)
//            {
//                return NotFound();
//            }

//            return View(approvalFlow);
//        }

//        // GET: ApprovalFlows/Create
//        public IActionResult Create()
//        {
//            ViewData["Id"] = new SelectList(_context.ApplicationUser, "Id", "Id");
//            ViewData["LeaveRequestId"] = new SelectList(_context.LeaveRequests, "LeaveRequestId", "LeaveRequestId");
//            return View();
//        }

//        // POST: ApprovalFlows/Create
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create([Bind("ApprovalFlowId,ApprovalOrder,Status,ActionDate,Remarks,LeaveRequestId,Id")] ApprovalFlow approvalFlow)
//        {
//            if (ModelState.IsValid)
//            {
//                _context.Add(approvalFlow);
//                await _context.SaveChangesAsync();
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["Id"] = new SelectList(_context.ApplicationUser, "Id", "Id", approvalFlow.Id);
//            ViewData["LeaveRequestId"] = new SelectList(_context.LeaveRequests, "LeaveRequestId", "LeaveRequestId", approvalFlow.LeaveRequestId);
//            return View(approvalFlow);
//        }

//        // GET: ApprovalFlows/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var approvalFlow = await _context.ApprovalFlows.FindAsync(id);
//            if (approvalFlow == null)
//            {
//                return NotFound();
//            }
//            ViewData["Id"] = new SelectList(_context.ApplicationUser, "Id", "Id", approvalFlow.Id);
//            ViewData["LeaveRequestId"] = new SelectList(_context.LeaveRequests, "LeaveRequestId", "LeaveRequestId", approvalFlow.LeaveRequestId);
//            return View(approvalFlow);
//        }

//        // POST: ApprovalFlows/Edit/5
       
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, [Bind("ApprovalFlowId,ApprovalOrder,Status,ActionDate,Remarks,LeaveRequestId,Id")] ApprovalFlow approvalFlow)
//        {
//            if (id != approvalFlow.ApprovalFlowId)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(approvalFlow);
//                    await _context.SaveChangesAsync();
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!ApprovalFlowExists(approvalFlow.ApprovalFlowId))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//                return RedirectToAction(nameof(Index));
//            }
//            ViewData["Id"] = new SelectList(_context.ApplicationUser, "Id", "Id", approvalFlow.Id);
//            ViewData["LeaveRequestId"] = new SelectList(_context.LeaveRequests, "LeaveRequestId", "LeaveRequestId", approvalFlow.LeaveRequestId);
//            return View(approvalFlow);
//        }

//        // GET: ApprovalFlows
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null)
//            {
//                return NotFound();
//            }

//            var approvalFlow = await _context.ApprovalFlows
//                .Include(a => a.ApplicationUser)
//                .Include(a => a.leaveRequest)
//                .FirstOrDefaultAsync(m => m.ApprovalFlowId == id);
//            if (approvalFlow == null)
//            {
//                return NotFound();
//            }

//            return View(approvalFlow);
//        }

//        // POST: ApprovalFlows
//        [HttpPost, ActionName("Delete")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var approvalFlow = await _context.ApprovalFlows.FindAsync(id);
//            if (approvalFlow != null)
//            {
//                _context.ApprovalFlows.Remove(approvalFlow);
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private bool ApprovalFlowExists(int id)
//        {
//            return _context.ApprovalFlows.Any(e => e.ApprovalFlowId == id);
//        }
//    }
//}

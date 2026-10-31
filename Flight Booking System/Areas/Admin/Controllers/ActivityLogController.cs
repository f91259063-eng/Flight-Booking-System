
using Flight_Booking_System.Data;
using Flight_Booking_System.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.Identity.ApplicationUser> _userManager;

        public ActivityLogController(
            ApplicationDbContext context,
            UserManager<Models.Identity.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? userId,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? control,
            string? actionType,
            int page = 1)
        {
            const int pageSize = 20;

            page = Math.Max(1, page);

            var model = new ActivityLogIndexViewModel
            {
                UserId = userId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Control = control,
                ActionType = actionType,
                CurrentPage = page,
                PageSize = pageSize
            };

            // =========================
            // Users Dropdown
            // =========================

            model.Users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName ?? u.Email ?? u.Id
                })
                .ToListAsync();

            // =========================
            // Controller Dropdown
            // =========================

            model.Controls = await _context.ActivityLogs
                .AsNoTracking()
                .Where(x => x.Control != "")
                .Select(x => x.Control)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x
                })
                .ToListAsync();

            // =========================
            // Action Dropdown
            // =========================

            model.Actions = await _context.ActivityLogs
                .AsNoTracking()
                .Where(x => x.Action != "")
                .Select(x => x.Action)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => new SelectListItem
                {
                    Value = x,
                    Text = x
                })
                .ToListAsync();

            // =========================
            // Search Query
            // =========================

            var query = _context.ActivityLogs
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            if (dateFrom.HasValue)
            {
                var from = dateFrom.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (dateTo.HasValue)
            {
                var to = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            if (!string.IsNullOrEmpty(control))
                query = query.Where(x => x.Control == control);

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(x => x.Action == actionType);

            // =========================
            // Pagination
            // =========================

            model.TotalRecords = await query.CountAsync();

            model.Logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(model);
        }
    }
}


using Flight_Booking_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Flight_Booking_System.ViewModels
{
    public class ActivityLogIndexViewModel
    {
        public List<ActivityLog> Logs { get; set; } = new();

        // Search Filters
        public string? UserId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Control { get; set; }
        public string? ActionType { get; set; }

        // Dropdown Lists
        public List<SelectListItem> Users { get; set; } = new();
        public List<SelectListItem> Controls { get; set; } = new();
        public List<SelectListItem> Actions { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalRecords { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}

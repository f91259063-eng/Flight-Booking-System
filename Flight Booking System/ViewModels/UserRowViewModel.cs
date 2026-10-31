namespace Flight_Booking_System.ViewModels
{
    // one line of the users table
    public class UserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsLocked { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    // the whole users page: the filter + the rows
    public class UserListViewModel
    {
        public List<UserRowViewModel> Users { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();

        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }

        public string CurrentUserId { get; set; } = string.Empty;

        // accounts that are not confirmed yet
        public int PendingCount { get; set; }
    }
}

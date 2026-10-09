namespace Flight_Booking_System.ViewModels
{
    public class UserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLocked { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}

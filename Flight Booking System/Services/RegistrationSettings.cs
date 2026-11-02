namespace Flight_Booking_System.Services
{
    // "Registration" section of appsettings.json
    public class RegistrationSettings
    {
        // "Admin": the administrator confirms new accounts from the users page (no email is sent).
        // "Email": the user confirms his account with the link in the email (use this in production).
        public string ConfirmationMode { get; set; } = "Admin";
    }
}

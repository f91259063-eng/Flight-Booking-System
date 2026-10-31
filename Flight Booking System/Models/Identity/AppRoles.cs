namespace Flight_Booking_System.Models.Identity
{
    // Role names in one place so we never mistype them
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Receptionist = "Receptionist";
        public const string BookingAgent = "BookingAgent";
        public const string Customer = "Customer";

        public static readonly string[] All = { Admin, Receptionist, BookingAgent, Customer };

        // for [Authorize(Roles = ...)]: the airline employees
        public const string StaffRoles = Admin + "," + Receptionist + "," + BookingAgent;

        // for [Authorize(Roles = ...)]: the people who can book a flight
        public const string BookingRoles = Customer + "," + BookingAgent + "," + Admin;
    }

    public static class AppPolicies
    {
        // Admin, Receptionist and BookingAgent (the airline employees)
        public const string StaffOnly = "StaffOnly";

        // Only people who are allowed to book on behalf of a customer
        public const string CanBookForCustomers = "CanBookForCustomers";
    }
}

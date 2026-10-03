using Microsoft.AspNetCore.Identity;

namespace Flight_Booking_System.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {

       public string FirstName { get; set; }
        public string MiddelName { get; set; }
        public string LastName { get; set; }




    }
}

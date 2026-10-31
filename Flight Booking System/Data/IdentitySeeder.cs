using Flight_Booking_System.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Flight_Booking_System.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Demo accounts for testing. Change these passwords before any real deployment.
            await EnsureUserAsync(userManager, "admin", "admin@flightbooking.com",
                "System", "Admin", "Admin@12345", AppRoles.Admin);

            await EnsureUserAsync(userManager, "reception", "reception@flightbooking.com",
                "Front", "Desk", "Reception@123", AppRoles.Receptionist);

            await EnsureUserAsync(userManager, "agent", "agent@flightbooking.com",
                "Booking", "Agent", "Agent@12345", AppRoles.BookingAgent);
        }

        private static async Task EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string userName, string email,
            string firstName, string lastName,
            string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    MiddelName = string.Empty,
                    LastName = lastName
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    return;
            }
            else if (user.UserName != userName)
            {
                // old demo accounts were created with the email as the username
                await userManager.SetUserNameAsync(user, userName);
            }

            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}

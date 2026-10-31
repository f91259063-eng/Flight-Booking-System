using Flight_Booking_System.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Flight_Booking_System.Data
{
    public static class IdentitySeeder
    {
        public static readonly string[] Roles = { "Admin", "Customer" };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Default admin for testing. Change this password before any real deployment.
            const string adminEmail = "admin@flightbooking.com";
            if (await userManager.FindByEmailAsync(adminEmail) is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    MiddelName = string.Empty,
                    LastName = "Admin"
                };

                var result = await userManager.CreateAsync(admin, "Admin@12345");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}

using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Users must confirm their email before they can log in
                options.SignIn.RequireConfirmedAccount = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();   // needed for email confirmation, password reset and 2FA tokens

            builder.Services.AddScoped<IRepository<city>, Repository<city>>();
            // Where to send users who are not logged in / not allowed
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // Email (settings come from the "Email" section in appsettings.json)
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
            builder.Services.AddTransient<IAppEmailSender, SmtpEmailSender>();




            // MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Create the roles (Admin, Customer) and a default admin on startup
            using (var scope = app.Services.CreateScope())
            {
                await IdentitySeeder.SeedAsync(scope.ServiceProvider);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // Authentication must be before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Areas route must come BEFORE the default route

            // Identity Area Route
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=identity}/{controller=Account}/{action=login}/{id?}")
          
            .WithStaticAssets();


            app.Run();

        }
    }
}

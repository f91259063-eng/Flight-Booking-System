using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ModelsServices;
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

            // Check the security stamp on every request, so locking a user or
            // changing his roles takes effect immediately (not after 30 minutes)
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.Zero;
            });

            // Where to send users who are not logged in / not allowed
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // Authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.StaffOnly, policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.Receptionist, AppRoles.BookingAgent));

                options.AddPolicy(AppPolicies.CanBookForCustomers, policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.BookingAgent));
            });

            // Generic repository (one registration for every entity)
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Services
            builder.Services.AddHttpClient<AirportCoordinateService>();
            builder.Services.AddScoped<DistanceCalculatorService>();
            builder.Services.AddScoped<ISeatService, SeatService>();

            // Activity log
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

            // Email (settings come from the "Email" section in appsettings.json)
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
            builder.Services.Configure<RegistrationSettings>(builder.Configuration.GetSection("Registration"));

            // Stripe (payment)
            builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
            builder.Services.AddTransient<IAppEmailSender, SmtpEmailSender>();

            // MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Create the roles and the demo accounts on startup
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
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            // "/" goes to HomeController, which sends visitors to the login page
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}


using Flight_Booking_System.Data;
using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;
using Flight_Booking_System.Services;
using Flight_Booking_System.Services.Rewards;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================================
            // DATABASE
            // =====================================================

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // =====================================================
            // IDENTITY
            // =====================================================

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedAccount = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Validate security stamp on every request.
            // Changes to roles and lockout take effect immediately.
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.ValidationInterval = TimeSpan.Zero;
            });

            // =====================================================
            // HTTP CONTEXT
            // =====================================================

            builder.Services.AddHttpContextAccessor();

            // =====================================================
            // REPOSITORIES
            // =====================================================

            builder.Services.AddScoped(
                typeof(IRepository<>),
                typeof(Repository<>));

            // =====================================================
            // APPLICATION SERVICES
            // =====================================================

            builder.Services.AddHttpClient<AirportCoordinateService>();

            builder.Services.AddScoped<DistanceCalculatorService>();

            builder.Services.AddScoped<ISeatService, SeatService>();

            // =====================================================
            // ACTIVITY LOG
            // =====================================================

            builder.Services.AddScoped<
                IActivityLogService,
                ActivityLogService>();

            // =====================================================
            // REWARD POINTS
            // =====================================================

            builder.Services.AddScoped<
                IRewardService,
                RewardService>();

            // =====================================================
            // EMAIL SETTINGS
            // =====================================================

            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("Email"));

            builder.Services.Configure<RegistrationSettings>(
                builder.Configuration.GetSection("Registration"));

            builder.Services.AddTransient<
                IAppEmailSender,
                SmtpEmailSender>();

            // =====================================================
            // STRIPE PAYMENT
            // =====================================================

            builder.Services.Configure<StripeSettings>(
                builder.Configuration.GetSection("Stripe"));

            var stripeSecretKey =
                builder.Configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(stripeSecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe SecretKey is missing. Check User Secrets configuration.");
            }

            // =====================================================
            // COOKIES
            // =====================================================

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // =====================================================
            // AUTHORIZATION POLICIES
            // =====================================================

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.StaffOnly, policy =>
                    policy.RequireRole(
                        AppRoles.Admin,
                        AppRoles.Receptionist,
                        AppRoles.BookingAgent));

                options.AddPolicy(AppPolicies.CanBookForCustomers, policy =>
                    policy.RequireRole(
                        AppRoles.Admin,
                        AppRoles.BookingAgent));
            });

            // =====================================================
            // MVC
            // =====================================================

            builder.Services.AddControllersWithViews();

            // =====================================================
            // BUILD APPLICATION
            // =====================================================

            var app = builder.Build();

            // =====================================================
            // SEED IDENTITY
            // =====================================================

            using (var scope = app.Services.CreateScope())
            {
                await IdentitySeeder.SeedAsync(
                    scope.ServiceProvider);
            }

            // =====================================================
            // MIDDLEWARE
            // =====================================================

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Areas route must come before the default route.
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}

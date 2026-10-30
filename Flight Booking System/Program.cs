

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

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
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

            // Repositories
            builder.Services.AddScoped<IRepository<Flight>, Repository<Flight>>();
            builder.Services.AddScoped<IRepository<Booking>, Repository<Booking>>();
            builder.Services.AddScoped<IRepository<Passenger>, Repository<Passenger>>();
            builder.Services.AddScoped<IRepository<Aircraft>, Repository<Aircraft>>();
            builder.Services.AddScoped<IRepository<AirPort>, Repository<AirPort>>();
            builder.Services.AddScoped<IRepository<Flight_Booking_System.Models.Route>,
                Repository<Flight_Booking_System.Models.Route>>();
            builder.Services.AddScoped<IRepository<Seat>, Repository<Seat>>();
            builder.Services.AddScoped<IRepository<Payment>, Repository<Payment>>();
            builder.Services.AddScoped<IRepository<city>, Repository<city>>();
            builder.Services.AddScoped<IRepository<AircraftClass>, Repository<AircraftClass>>();
            builder.Services.AddScoped<IRepository<Class>, Repository<Class>>();
            builder.Services.AddScoped<IRepository<Airline>, Repository<Airline>>();
            builder.Services.AddScoped<IRepository<Setting>, Repository<Setting>>();
            builder.Services.AddScoped<IRepository<Ticket>, Repository<Ticket>>();

            // Activity Log
            builder.Services.AddScoped<IRepository<ActivityLog>,
                Repository<ActivityLog>>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IActivityLogService,
                ActivityLogService>();

            // Other services
            builder.Services.AddHttpClient<AirportCoordinateService>();
            builder.Services.AddScoped<DistanceCalculatorService>();
            builder.Services.AddScoped<IRewardService, RewardService>();

            builder.Services.AddScoped<
                Flight_Booking_System.Services.ISeatService,
                SeatService>();

            // Cookies
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            // Email
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("Email"));

            builder.Services.AddTransient<IAppEmailSender, SmtpEmailSender>();

            // Stripe
            var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];

            if (string.IsNullOrWhiteSpace(stripeSecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe SecretKey is missing. Check User Secrets configuration.");
            }

            builder.Services.Configure<StripeSettings>(
                builder.Configuration.GetSection("Stripe"));
            // MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Seed Identity
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            // Identity / Default route
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=identity}/{controller=Account}/{action=login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}

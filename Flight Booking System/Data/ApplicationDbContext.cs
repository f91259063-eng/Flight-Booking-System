using Flight_Booking_System.Models;
using Flight_Booking_System.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Flight_Booking_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { 
        }
        public DbSet<city> Cities { get; set;  }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Aircraft> Aircrafts { get; set; }
        public DbSet<AircraftClass> AircraftClasses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<AirPort> AirPorts { get; set; }
        public DbSet<Flight_Booking_System.Models.Route> Routes { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // Route → From Airport
            // =========================

            modelBuilder.Entity<Flight_Booking_System.Models.Route>()
                .HasOne(r => r.FromAirport)
                .WithMany(a => a.FromRoutes)
                .HasForeignKey(r => r.FromAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // Route → To Airport
            // =========================

            modelBuilder.Entity<Flight_Booking_System.Models. Route>()
                .HasOne(r => r.ToAirport)
                .WithMany(a => a.ToRoutes)
                .HasForeignKey(r => r.ToAirportId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // Aircraft → Airline
            // =========================

            modelBuilder.Entity<Aircraft>()
                .HasOne(a => a.Airline)
                .WithMany()
                .HasForeignKey(a => a.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // Flight → Airline
            // =========================

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.Airline)
                .WithMany()
                .HasForeignKey(f => f.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // Flight → Aircraft
            // =========================

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.Aircraft)
                .WithMany()
                .HasForeignKey(f => f.AircraftId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // Flight → Route
            // =========================

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.Route)
                .WithMany()
                .HasForeignKey(f => f.RouteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
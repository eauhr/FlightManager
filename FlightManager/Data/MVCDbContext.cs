using FlightManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data
{
    public class MVCDbContext : IdentityDbContext<User>
    {
        public MVCDbContext(DbContextOptions<MVCDbContext> options) : base(options)
        {
        }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Passenger> Passengers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(e => {
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => u.EGN).IsUnique();
                e.Property(u => u.EGN).HasMaxLength(10);
            });

            modelBuilder.Entity<Flight>(e => {
                e.HasIndex(f => f.PlaneNumber).IsUnique();
            });

            modelBuilder.Entity<Passenger>(e => {
                e.HasIndex(p => p.EGN).IsUnique();
                e.Property(p => p.EGN).HasMaxLength(10);
                e.Property(p => p.Type).HasConversion<string>();
            });

        }



    }
 }


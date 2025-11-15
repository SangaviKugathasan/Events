using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EventZax.Models;
using System;

namespace EventZax.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Ensure database directory exists
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            Directory.CreateDirectory(path);
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<TicketTier> TicketTiers { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Event entity configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasColumnType("TEXT");
                entity.Property(e => e.Category).IsRequired().HasColumnType("TEXT");
                entity.Property(e => e.VenueName).IsRequired().HasColumnType("TEXT").HasDefaultValue("");
                entity.Property(e => e.StartDate).IsRequired().HasColumnType("TEXT");
                entity.Property(e => e.EndDate).HasColumnType("TEXT"); // allow nulls
                entity.Property(e => e.IsPublished).HasColumnType("INTEGER").HasDefaultValue(false);
                entity.Property(e => e.OrganizerId).HasColumnType("TEXT");
                entity.Property(e => e.ImagePath).HasColumnType("TEXT").HasDefaultValue("");
            });

            // Seed venues
            modelBuilder.Entity<Venue>().HasData(
                new Venue { Id = 1, Name = "Main Arena", Address = "123 Main St", City = "Downtown", Capacity = 5000 },
                new Venue { Id = 2, Name = "Convention Center", Address = "456 Center Ave", City = "Midtown", Capacity = 3000 }
            );

            // Seed events (use VenueName rather than VenueId)
            modelBuilder.Entity<Event>().HasData(
                new Event
                {
                    Id = 1,
                    Title = "Tech Conference 2025",
                    Category = "Technology",
                    VenueName = "Main Arena",
                    StartDate = new DateTime(2025, 10, 1),
                    EndDate = new DateTime(2025, 10, 3),
                    IsPublished = true,
                    OrganizerId = "admin",
                    ImagePath = ""
                },
                new Event
                {
                    Id = 2,
                    Title = "Music Festival 2025",
                    Category = "Music",
                    VenueName = "Convention Center",
                    StartDate = new DateTime(2025, 11, 15),
                    EndDate = new DateTime(2025, 11, 17),
                    IsPublished = true,
                    OrganizerId = "admin",
                    ImagePath = ""
                }
            );

            // Seed ticket tiers - these reference EventId (no change)
            modelBuilder.Entity<TicketTier>().HasData(
                new TicketTier { Id = 1, EventId = 1, Name = "Early Bird", Price = 299.99M, QuantityAvailable = 1000 },
                new TicketTier { Id = 2, EventId = 1, Name = "Regular", Price = 499.99M, QuantityAvailable = 2000 },
                new TicketTier { Id = 3, EventId = 2, Name = "General Admission", Price = 150M, QuantityAvailable = 2500 },
                new TicketTier { Id = 4, EventId = 2, Name = "VIP", Price = 299.99M, QuantityAvailable = 500 }
            );

            // Attendance relationships
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Event)
                .WithMany()
                .HasForeignKey(a => a.EventId);
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId);
        }
    }
}
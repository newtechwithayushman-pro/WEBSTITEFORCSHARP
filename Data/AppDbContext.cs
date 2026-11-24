using CollegeEventManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CollegeEventManagement.Data
{
    // Represents our database. Each DbSet is a table.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CollegeEvent> CollegeEvents { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Each email can only be used once
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Database-level rule: the same student cannot register for the same event twice
            modelBuilder.Entity<Registration>().HasIndex(r => new { r.UserId, r.CollegeEventId }).IsUnique();

            // Every QR token is unique
            modelBuilder.Entity<Registration>().HasIndex(r => r.QrToken).IsUnique();

            // One attendance record per registration (a student cannot be checked in twice)
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Registration)
                .WithOne(r => r.Attendance)
                .HasForeignKey<Attendance>(a => a.RegistrationId);

            // Deleting an organiser account must not silently delete their events
            // (this also avoids SQL Server's "multiple cascade paths" error)
            modelBuilder.Entity<CollegeEvent>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        // Events with everything the pages need (organiser, registrations, students, attendance).
        // Always read navigation properties from entities loaded through these two helpers.
        public IQueryable<CollegeEvent> EventsFull() =>
            CollegeEvents
                .Include(e => e.Organizer)
                .Include(e => e.Registrations).ThenInclude(r => r.Attendance)
                .Include(e => e.Registrations).ThenInclude(r => r.User);

        public IQueryable<Registration> RegistrationsFull() =>
            Registrations
                .Include(r => r.User)
                .Include(r => r.Attendance)
                .Include(r => r.CollegeEvent).ThenInclude(e => e.Registrations);
    }
}

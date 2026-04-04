using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SmartVillageAPI.Model
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Zone> Zones { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<AutomationSetting> AutomationSettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ParkingReservation> ParkingReservations { get; set; }
        public DbSet<WasteCollectionRequest> WasteCollectionRequests { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Recording> Recordings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // بيانات Zones
            builder.Entity<Zone>().HasData(
                new Zone { Id = 1, Name = "Smart Home", Icon = "home", IsPublic = false, Description = "any" },
                new Zone { Id = 2, Name = "Agriculture", Icon = "leaf", IsPublic = false, Description = "any" },
                new Zone { Id = 3, Name = "Parking", Icon = "local_parking", IsPublic = true, Description = "any" },
                new Zone { Id = 4, Name = "Energy", Icon = "bolt", IsPublic = true, Description = "any" },
                new Zone { Id = 5, Name = "Surveillance", Icon = "videocam", IsPublic = true, Description = "any" },
                new Zone { Id = 6, Name = "Waste Mgmt", Icon = "delete", IsPublic = true, Description = "any" },
                new Zone { Id = 7, Name = "Umbrella", Icon = "umbrella", IsPublic = true, Description = "any" },
                new Zone { Id = 8, Name = "Emergency", Icon = "warning", IsPublic = true, Description = "any" },
                new Zone { Id = 9, Name = "Camera", Icon = "gate", IsPublic = true, Description = "any" }
            );

            builder.Entity<Zone>()
                .HasOne(z => z.AutomationSetting)
                .WithOne(a => a.Zone)
                .HasForeignKey<AutomationSetting>(a => a.ZoneId);
        }
    }
}
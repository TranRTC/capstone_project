using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<SensorReading> SensorReadings { get; set; }
        public DbSet<DeviceStatusHistory> DeviceStatusHistories { get; set; }
        public DbSet<OperationalMetric> OperationalMetrics { get; set; }
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<DeviceConfiguration> DeviceConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Sensor>()
                .HasOne(s => s.Device)
                .WithMany(d => d.Sensors)
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasOne(sr => sr.Device)
                .WithMany(d => d.SensorReadings)
                .HasForeignKey(sr => sr.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SensorReading>()
                .HasOne(sr => sr.Sensor)
                .WithMany(s => s.SensorReadings)
                .HasForeignKey(sr => sr.SensorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DeviceStatusHistory>()
                .HasOne(dsh => dsh.Device)
                .WithMany(d => d.DeviceStatusHistories)
                .HasForeignKey(dsh => dsh.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationalMetric>()
                .HasOne(om => om.Device)
                .WithMany(d => d.OperationalMetrics)
                .HasForeignKey(om => om.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AlertRule>()
                .HasOne(ar => ar.Device)
                .WithMany(d => d.AlertRules)
                .HasForeignKey(ar => ar.DeviceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AlertRule>()
                .HasOne(ar => ar.Sensor)
                .WithMany(s => s.AlertRules)
                .HasForeignKey(ar => ar.SensorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.AlertRule)
                .WithMany(ar => ar.Alerts)
                .HasForeignKey(a => a.AlertRuleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Device)
                .WithMany()
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Sensor)
                .WithMany()
                .HasForeignKey(a => a.SensorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DeviceConfiguration>()
                .HasOne(dc => dc.Device)
                .WithMany(d => d.DeviceConfigurations)
                .HasForeignKey(dc => dc.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<SensorReading>()
                .HasIndex(sr => new { sr.Timestamp, sr.DeviceId });

            modelBuilder.Entity<SensorReading>()
                .HasIndex(sr => new { sr.DeviceId, sr.SensorId, sr.Timestamp });

            modelBuilder.Entity<DeviceStatusHistory>()
                .HasIndex(dsh => new { dsh.DeviceId, dsh.Timestamp });

            modelBuilder.Entity<Device>()
                .HasIndex(d => d.EdgeDeviceId)
                .IsUnique()
                .HasFilter("[EdgeDeviceId] IS NOT NULL");
        }
    }
}


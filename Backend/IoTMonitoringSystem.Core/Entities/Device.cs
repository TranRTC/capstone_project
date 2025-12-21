using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(50)]
        public string? EdgeDeviceType { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSeenAt { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation Properties
        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
        public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
        public ICollection<DeviceStatusHistory> DeviceStatusHistories { get; set; } = new List<DeviceStatusHistory>();
        public ICollection<OperationalMetric> OperationalMetrics { get; set; } = new List<OperationalMetric>();
        public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
        public ICollection<DeviceConfiguration> DeviceConfigurations { get; set; } = new List<DeviceConfiguration>();
    }
}


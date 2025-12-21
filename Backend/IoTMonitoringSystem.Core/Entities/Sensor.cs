using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class Sensor
    {
        [Key]
        public int SensorId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SensorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SensorType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxValue { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
        public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
    }
}


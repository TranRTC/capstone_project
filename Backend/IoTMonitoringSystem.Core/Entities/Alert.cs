using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class Alert
    {
        [Key]
        public long AlertId { get; set; }

        [Required]
        public int AlertRuleId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        public int? SensorId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal? TriggerValue { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

        public DateTime? AcknowledgedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("AlertRuleId")]
        public AlertRule AlertRule { get; set; } = null!;

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        [ForeignKey("SensorId")]
        public Sensor? Sensor { get; set; }
    }
}


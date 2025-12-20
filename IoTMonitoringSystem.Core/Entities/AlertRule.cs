using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AlertRule
    {
        [Key]
        public int AlertRuleId { get; set; }

        public int? DeviceId { get; set; }

        public int? SensorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RuleType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Condition { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal? ThresholdValue { get; set; }

        [MaxLength(10)]
        public string? ComparisonOperator { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device? Device { get; set; }

        [ForeignKey("SensorId")]
        public Sensor? Sensor { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}


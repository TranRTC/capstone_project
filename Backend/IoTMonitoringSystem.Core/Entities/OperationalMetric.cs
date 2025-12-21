using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class OperationalMetric
    {
        [Key]
        public long MetricId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MetricType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal MetricValue { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        [MaxLength(20)]
        public string? CalculationPeriod { get; set; }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}


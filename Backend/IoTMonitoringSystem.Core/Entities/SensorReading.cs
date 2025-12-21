using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class SensorReading
    {
        [Key]
        public long ReadingId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Value { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(20)]
        public string? Quality { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        [ForeignKey("SensorId")]
        public Sensor Sensor { get; set; } = null!;
    }
}


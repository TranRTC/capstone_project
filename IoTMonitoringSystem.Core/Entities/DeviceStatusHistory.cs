using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class DeviceStatusHistory
    {
        [Key]
        public long StatusHistoryId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        public int? StatusCode { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}


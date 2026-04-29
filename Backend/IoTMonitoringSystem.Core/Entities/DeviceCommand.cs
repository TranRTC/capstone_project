using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class DeviceCommand
    {
        [Key]
        public long CommandId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CommandType { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Payload { get; set; } = "{}";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        [MaxLength(100)]
        public string? RequestedBy { get; set; }

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}

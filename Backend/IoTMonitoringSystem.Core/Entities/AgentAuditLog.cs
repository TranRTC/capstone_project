using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AgentAuditLog
    {
        [Key]
        public long AgentAuditLogId { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? UserRole { get; set; }

        [MaxLength(80)]
        public string? ToolName { get; set; }

        [MaxLength(500)]
        public string? Summary { get; set; }

        [MaxLength(4000)]
        public string? DetailsJson { get; set; }

        public int? RelatedDeviceId { get; set; }
        public long? RelatedAlertId { get; set; }
        public long? SessionId { get; set; }
        public int? DurationMs { get; set; }
        public bool Success { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

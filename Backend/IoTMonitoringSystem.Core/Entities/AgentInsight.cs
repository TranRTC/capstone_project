using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AgentInsight
    {
        [Key]
        public long AgentInsightId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TriggerType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DedupeKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "info";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Summary { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? SuggestedActionsJson { get; set; }

        public long? RelatedAlertId { get; set; }
        public int? RelatedDeviceId { get; set; }

        [MaxLength(1000)]
        public string? ToolsUsedJson { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public bool UsedLlm { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DismissedAt { get; set; }
    }
}

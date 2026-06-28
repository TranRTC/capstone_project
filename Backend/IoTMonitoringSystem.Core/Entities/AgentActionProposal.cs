using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AgentActionProposal
    {
        [Key]
        public long AgentActionProposalId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string PayloadJson { get; set; } = "{}";

        [Required]
        [MaxLength(500)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(4000)]
        public string? ResultJson { get; set; }

        public int? RelatedDeviceId { get; set; }
        public long? RelatedAlertId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }
}

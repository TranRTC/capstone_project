using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AgentChatMessage
    {
        [Key]
        public long AgentChatMessageId { get; set; }

        public long AgentChatSessionId { get; set; }
        public AgentChatSession Session { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ToolsUsedJson { get; set; }

        public DateTime? DataAsOfUtc { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

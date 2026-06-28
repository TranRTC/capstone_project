using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class AgentChatSession
    {
        [Key]
        public long AgentChatSessionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Title { get; set; }

        public int? ContextDeviceId { get; set; }
        public long? ContextAlertId { get; set; }

        [MaxLength(120)]
        public string? ContextRoute { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AgentChatMessage> Messages { get; set; } = new List<AgentChatMessage>();
    }
}

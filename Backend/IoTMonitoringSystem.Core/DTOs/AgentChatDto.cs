using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentChatRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = string.Empty;

        public List<AgentChatMessageDto>? History { get; set; }

        public long? SessionId { get; set; }

        public AgentChatContextDto? Context { get; set; }
    }

    public class AgentChatMessageDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class AgentChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public List<string> ToolsUsed { get; set; } = new();
        public List<string> DocSourcesUsed { get; set; } = new();
        public AgentActionProposalDto? PendingAction { get; set; }
        public long? SessionId { get; set; }
        public DateTime? DataAsOfUtc { get; set; }
        public AgentChatContextDto? ContextUsed { get; set; }
        public bool UsedIntentRouter { get; set; }
    }
}

namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentChatContextDto
    {
        public int? DeviceId { get; set; }
        public long? AlertId { get; set; }
        public string? Route { get; set; }
    }

    public class AgentChatSessionDto
    {
        public long SessionId { get; set; }
        public string? Title { get; set; }
        public AgentChatContextDto? Context { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AgentChatMessageDto> Messages { get; set; } = new();
    }

    public class AgentAuditLogDto
    {
        public long AgentAuditLogId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? UserRole { get; set; }
        public string? ToolName { get; set; }
        public string? Summary { get; set; }
        public int? RelatedDeviceId { get; set; }
        public long? RelatedAlertId { get; set; }
        public int? DurationMs { get; set; }
        public bool Success { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AgentMetricsDto
    {
        public int ChatRequestsLast24h { get; set; }
        public int ToolCallsLast24h { get; set; }
        public int ActionsConfirmedLast24h { get; set; }
        public int LoopLimitHitsLast24h { get; set; }
        public int LlmErrorsLast24h { get; set; }
        public double AverageChatDurationMs { get; set; }
        public List<AgentToolUsageDto> TopTools { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class AgentToolUsageDto
    {
        public string ToolName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AgentSuggestedPromptsDto
    {
        public List<string> Prompts { get; set; } = new();
    }
}

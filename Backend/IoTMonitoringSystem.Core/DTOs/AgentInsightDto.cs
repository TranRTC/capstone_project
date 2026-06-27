namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentInsightDto
    {
        public long AgentInsightId { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> SuggestedActions { get; set; } = new();
        public long? RelatedAlertId { get; set; }
        public int? RelatedDeviceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool UsedLlm { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DismissedAt { get; set; }
        public string? ChatSeedMessage { get; set; }
    }

    public class AgentInsightQueryDto
    {
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AgentProactiveStatusDto
    {
        public bool Enabled { get; set; }
        public bool LlmConfigured { get; set; }
        public int ActiveInsightCount { get; set; }
        public DateTime? LastSweepAtUtc { get; set; }
    }

    public class AgentOpenInChatResponse
    {
        public string SeedMessage { get; set; } = string.Empty;
    }
}

namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentActionProposalDto
    {
        public long AgentActionProposalId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? RelatedDeviceId { get; set; }
        public long? RelatedAlertId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool CanConfirm { get; set; }
    }

    public class AgentActionResultDto
    {
        public long AgentActionProposalId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ResultJson { get; set; }
    }
}

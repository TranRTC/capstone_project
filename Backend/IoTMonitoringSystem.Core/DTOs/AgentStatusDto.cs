namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentStatusDto
    {
        public bool Enabled { get; set; }
        public bool Configured { get; set; }
        public string Model { get; set; } = string.Empty;
        public string? SetupHint { get; set; }
    }
}

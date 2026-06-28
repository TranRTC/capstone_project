namespace IoTMonitoringSystem.Core.DTOs
{
    public class AgentMcpStatusDto
    {
        public bool Enabled { get; set; }
        public string HttpPath { get; set; } = "/mcp";
        public bool RequireApiKey { get; set; }
        public IReadOnlyList<string> ToolNames { get; set; } = Array.Empty<string>();
        public string SetupHint { get; set; } = string.Empty;
    }
}

namespace IoTMonitoringSystem.API.Services
{
    public interface ILlmClient
    {
        bool IsConfigured { get; }
        Task<LlmChatResult> CompleteAsync(IReadOnlyList<LlmMessage> messages, IReadOnlyList<LlmToolDefinition> tools, CancellationToken cancellationToken = default);
    }

    public sealed class LlmMessage
    {
        public string Role { get; init; } = string.Empty;
        public string? Content { get; init; }
        public string? ToolCallId { get; init; }
        public IReadOnlyList<LlmToolCall>? ToolCalls { get; init; }
    }

    public sealed class LlmToolCall
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string ArgumentsJson { get; init; } = "{}";
    }

    public sealed class LlmToolDefinition
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public object ParametersSchema { get; init; } = new { type = "object", properties = new { } };
    }

    public sealed class LlmChatResult
    {
        public string? Content { get; init; }
        public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = Array.Empty<LlmToolCall>();
        public bool HasToolCalls => ToolCalls.Count > 0;
    }
}

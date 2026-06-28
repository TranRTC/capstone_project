namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Dispatches in-process assistant tool calls to <see cref="IIotAgentToolService"/>.
    /// </summary>
    public class AgentToolExecutor
    {
        private readonly IIotAgentToolService _tools;

        public AgentToolExecutor(IIotAgentToolService tools) => _tools = tools;

        public Task<string> ExecuteAsync(string toolName, string argumentsJson) =>
            _tools.ExecuteAsync(toolName, argumentsJson);
    }
}

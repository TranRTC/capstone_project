using System.Security.Claims;
using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentService : IAgentService
    {
        private const int MaxToolIterations = 5;

        private readonly ILlmClient _llmClient;
        private readonly AgentToolExecutor _toolExecutor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;

        public AgentService(
            ILlmClient llmClient,
            AgentToolExecutor toolExecutor,
            IConfiguration configuration,
            ILogger<AgentService> logger)
        {
            _llmClient = llmClient;
            _toolExecutor = toolExecutor;
            _configuration = configuration;
            _logger = logger;
        }

        public AgentStatusDto GetStatus()
        {
            var enabled = _configuration.GetValue("Agent:Enabled", true);
            var configured = _llmClient.IsConfigured;
            return new AgentStatusDto
            {
                Enabled = enabled,
                Configured = configured,
                Model = _configuration["Agent:Model"] ?? "gpt-4o-mini",
                SetupHint = configured
                    ? null
                    : IsOllamaDev()
                        ? "Install Ollama (https://ollama.com), run `ollama pull llama3.2`, start Ollama, then restart this API."
                        : "Set Agent:Llm:ApiKey via user-secrets or OPENAI_API_KEY, then restart the API."
            };
        }

        private bool IsOllamaDev()
        {
            var baseUrl = _configuration["Agent:Llm:BaseUrl"] ?? string.Empty;
            return baseUrl.Contains("11434", StringComparison.Ordinal);
        }

        public async Task<AgentChatResponse> ChatAsync(
            AgentChatRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            if (!_configuration.GetValue("Agent:Enabled", true))
                throw new InvalidOperationException("The AI assistant is disabled.");

            if (!_llmClient.IsConfigured)
                throw new InvalidOperationException("The AI assistant is not configured. Set Agent:Llm:ApiKey.");

            var role = user.FindFirst("role")?.Value
                ?? user.FindFirst(ClaimTypes.Role)?.Value
                ?? "Viewer";

            var maxHistory = _configuration.GetValue("Agent:MaxHistoryMessages", 10);
            var messages = BuildMessages(request, role, maxHistory);
            var tools = AgentToolDefinitions.BuildReadOnlyTools();
            var toolsUsed = new List<string>();

            var knownToolNames = tools.Select(t => t.Name).ToList();

            for (var iteration = 0; iteration < MaxToolIterations; iteration++)
            {
                var result = await _llmClient.CompleteAsync(messages, tools, cancellationToken);
                var toolCalls = ResolveToolCalls(result, knownToolNames);

                if (toolCalls.Count == 0)
                {
                    var reply = string.IsNullOrWhiteSpace(result.Content)
                        ? "I could not generate a response. Please try again."
                        : result.Content.Trim();

                    if (AgentToolCallParser.LooksLikeToolCallText(reply))
                    {
                        messages.Add(new LlmMessage
                        {
                            Role = "user",
                            Content = "Answer the question in plain English for the operator. Do not output JSON, tool names, or function syntax."
                        });
                        continue;
                    }

                    return new AgentChatResponse
                    {
                        Reply = reply,
                        ToolsUsed = toolsUsed.Distinct().ToList()
                    };
                }

                messages.Add(new LlmMessage
                {
                    Role = "assistant",
                    Content = result.Content,
                    ToolCalls = toolCalls
                });

                foreach (var toolCall in toolCalls)
                {
                    toolsUsed.Add(toolCall.Name);
                    var toolResult = await _toolExecutor.ExecuteAsync(toolCall.Name, toolCall.ArgumentsJson);
                    messages.Add(new LlmMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = toolResult
                    });
                }
            }

            _logger.LogWarning("Agent tool loop exceeded {Max} iterations", MaxToolIterations);
            return new AgentChatResponse
            {
                Reply = "I need too many data lookups to answer that question. Please try a simpler request.",
                ToolsUsed = toolsUsed.Distinct().ToList()
            };
        }

        private static List<LlmMessage> BuildMessages(AgentChatRequest request, string role, int maxHistory)
        {
            var messages = new List<LlmMessage>
            {
                new()
                {
                    Role = "system",
                    Content = $"""
                        You are an IoT monitoring assistant for the Web-Based IoT Device Real-Time Monitoring System dashboard.
                        The current user role is: {role}.
                        Use the provided tools to fetch real device, sensor, alert, and health data before answering factual questions.
                        Never invent sensor values, device status, or alert details.
                        If tools return empty results or errors, say you do not have that information.
                        Keep answers concise and practical for operators monitoring equipment.
                        When listing devices or alerts, include IDs and key status fields.
                        For setup or configuration questions, explain the workflow in plain English:
                        register the device in the dashboard, add sensors/actuators, configure MQTT topics and alert thresholds,
                        then deploy edge firmware that publishes readings to the broker. Use tools to show the user's current devices when helpful.
                        For actuator on/off status, call get_actuators_by_device and report LastKnownState (on/off) and LastStateAt for each actuator.
                        Discrete actuators use on/off; analog actuators report their last numeric value.
                        Never output raw tool-call JSON, function names, or API syntax to the user.
                        When calling tools that need deviceId, use the numeric ID from get_devices, not the device name.
                        """
                }
            };

            if (request.History is { Count: > 0 })
            {
                foreach (var item in request.History.TakeLast(maxHistory))
                {
                    if (string.IsNullOrWhiteSpace(item.Content))
                        continue;

                    var normalizedRole = item.Role?.ToLowerInvariant() switch
                    {
                        "assistant" => "assistant",
                        "user" => "user",
                        _ => null
                    };
                    if (normalizedRole is null)
                        continue;

                    messages.Add(new LlmMessage { Role = normalizedRole, Content = item.Content.Trim() });
                }
            }

            messages.Add(new LlmMessage { Role = "user", Content = request.Message.Trim() });
            return messages;
        }

        private static IReadOnlyList<LlmToolCall> ResolveToolCalls(LlmChatResult result, IReadOnlyList<string> knownToolNames)
        {
            if (result.HasToolCalls)
                return result.ToolCalls;

            return AgentToolCallParser.TryParseFromContent(result.Content, knownToolNames);
        }
    }
}

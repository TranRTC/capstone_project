using System.Security.Claims;
using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentService : IAgentService
    {
        private const int MaxToolIterations = 8;

        private readonly ILlmClient _llmClient;
        private readonly AgentToolExecutor _toolExecutor;
        private readonly IAgentActionService _agentActionService;
        private readonly AgentToggleIntentHandler _toggleIntentHandler;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;

        public AgentService(
            ILlmClient llmClient,
            AgentToolExecutor toolExecutor,
            IAgentActionService agentActionService,
            AgentToggleIntentHandler toggleIntentHandler,
            IConfiguration configuration,
            ILogger<AgentService> logger)
        {
            _llmClient = llmClient;
            _toolExecutor = toolExecutor;
            _agentActionService = agentActionService;
            _toggleIntentHandler = toggleIntentHandler;
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

            var role = GetRole(user);
            var writeActionsEnabled = _configuration.GetValue("Agent:Actions:Enabled", true);
            var canWrite = writeActionsEnabled && role is "Admin" or "Operator";
            var ragEnabled = _configuration.GetValue("Agent:Rag:Enabled", true);

            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                var directActuator = await TryDirectActuatorCommandResponseAsync(request.Message, user, cancellationToken);
                if (directActuator is not null)
                    return directActuator;
            }

            // Actuator on/off/toggle is handled server-side; do not fall through to the LLM (avoids asking user to verify device info).
            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                return new AgentChatResponse
                {
                    Reply = "I could not match the device and actuator. Try: \"Turn off actuator 1 on device 1\".",
                    ToolsUsed = new List<string>()
                };
            }

            var maxHistory = _configuration.GetValue("Agent:MaxHistoryMessages", 10);
            var messages = BuildMessages(request, role, canWrite, ragEnabled, maxHistory);
            var tools = BuildTools(canWrite, ragEnabled);
            var toolsUsed = new List<string>();
            var docSourcesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AgentActionProposalDto? pendingAction = null;

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

                    if (pendingAction is not null &&
                        (AgentToolCallParser.LooksLikeProposalJson(reply) ||
                         AgentToolCallParser.LooksLikeToolCallText(reply)))
                    {
                        reply = BuildProposalUserReply(pendingAction);
                    }
                    else if (AgentToolCallParser.LooksLikeToolCallText(reply))
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
                        ToolsUsed = toolsUsed.Distinct().ToList(),
                        DocSourcesUsed = docSourcesUsed.ToList(),
                        PendingAction = pendingAction
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
                    string toolResult;

                    if (AgentWriteToolDefinitions.IsProposeTool(toolCall.Name))
                    {
                        try
                        {
                            pendingAction = await _agentActionService.CreateProposalFromToolCallAsync(
                                toolCall, user, cancellationToken);
                            toolResult = JsonSerializer.Serialize(new
                            {
                                success = true,
                                proposalId = pendingAction.AgentActionProposalId,
                                summary = pendingAction.Summary,
                                expiresAt = pendingAction.ExpiresAt,
                                message = "Action proposal created. The user must confirm in the dashboard before it runs."
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create action proposal from tool {Tool}", toolCall.Name);
                            toolResult = JsonSerializer.Serialize(new { success = false, error = ex.Message });
                        }
                    }
                    else
                    {
                        toolResult = await _toolExecutor.ExecuteAsync(toolCall.Name, toolCall.ArgumentsJson);
                        if (toolCall.Name == "search_documentation")
                            CollectDocSources(toolResult, docSourcesUsed);
                    }

                    messages.Add(new LlmMessage
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Content = toolResult
                    });
                }

                if (pendingAction is not null &&
                    toolCalls.Any(tc => AgentWriteToolDefinitions.IsProposeTool(tc.Name)))
                {
                    return new AgentChatResponse
                    {
                        Reply = BuildProposalUserReply(pendingAction),
                        ToolsUsed = toolsUsed.Distinct().ToList(),
                        DocSourcesUsed = docSourcesUsed.ToList(),
                        PendingAction = pendingAction
                    };
                }
            }

            _logger.LogWarning("Agent tool loop exceeded {Max} iterations", MaxToolIterations);

            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                var directToggle = await TryDirectActuatorCommandResponseAsync(request.Message, user, cancellationToken);
                if (directToggle is not null)
                    return directToggle;
            }

            return new AgentChatResponse
            {
                Reply = AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message)
                    ? "I could not match the device and actuator. Try: \"Turn on actuator 1 on device 1\" or \"Toggle actuator Cyliner on device 1\"."
                    : "I need too many data lookups to answer that question. Please try a simpler request.",
                ToolsUsed = toolsUsed.Distinct().ToList(),
                DocSourcesUsed = docSourcesUsed.ToList(),
                PendingAction = pendingAction
            };
        }

        private async Task<AgentChatResponse?> TryDirectActuatorCommandResponseAsync(
            string message,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            var result = await _toggleIntentHandler.ResolveActuatorCommandAsync(message, user, cancellationToken);
            if (!result.Handled)
                return null;

            if (result.Proposal is not null)
            {
                return new AgentChatResponse
                {
                    Reply = BuildProposalUserReply(result.Proposal),
                    ToolsUsed = new List<string>
                    {
                        result.Proposal.ActionType == "SendDeviceCommand"
                            ? "propose_send_device_command"
                            : "propose_toggle_actuator"
                    },
                    PendingAction = result.Proposal
                };
            }

            return new AgentChatResponse
            {
                Reply = result.Reply ?? "Could not process actuator command.",
                ToolsUsed = new List<string>()
            };
        }

        private static void CollectDocSources(string toolResultJson, ISet<string> docSourcesUsed)
        {
            try
            {
                using var doc = JsonDocument.Parse(toolResultJson);
                if (!doc.RootElement.TryGetProperty("results", out var results) ||
                    results.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var item in results.EnumerateArray())
                {
                    if (item.TryGetProperty("source", out var sourceProp))
                    {
                        var source = sourceProp.GetString();
                        if (!string.IsNullOrWhiteSpace(source))
                            docSourcesUsed.Add(source);
                    }
                }
            }
            catch
            {
                // Best-effort metadata for UI chip.
            }
        }

        private List<LlmToolDefinition> BuildTools(bool canWrite, bool ragEnabled)
        {
            var tools = AgentToolDefinitions.BuildReadOnlyTools();
            if (!ragEnabled)
                tools = tools.Where(t => t.Name != "search_documentation").ToList();
            if (canWrite)
                tools.AddRange(AgentWriteToolDefinitions.BuildProposeTools());
            return tools;
        }

        private static List<LlmMessage> BuildMessages(
            AgentChatRequest request,
            string role,
            bool canWrite,
            bool ragEnabled,
            int maxHistory)
        {
            var writeInstructions = canWrite
                ? """
                  You may propose write actions using propose_acknowledge_alert, propose_resolve_alert, propose_create_device, propose_send_device_command, or propose_toggle_actuator.
                  When the user asks to toggle an actuator: call get_actuators_by_device, then call propose_toggle_actuator with deviceId and actuatorId. Do not check sensor thresholds — toggling actuators is unrelated to alerts.
                  LastKnownState may be "on", "off", "1", "0", or a number — get_actuators_by_device includes parsedIsOn and toggleWouldSetOn to help you.
                  If get_actuators_by_device lists the actuator, you have the information — never say you lack actuator data.
                  For turn on/off (not toggle), use propose_send_device_command with SetPower (on=true/false).
                  Turn on/off/toggle requests are usually handled by the server automatically — do not call get_device to ask the user to verify device location or name.
                  Never ask "is this correct?" or "please let me know if this is correct" for actuator commands.
                  Only one pending action is allowed per user.
                  Never claim an action was executed until the user confirms it in the dashboard.
                  When you propose an action, explain what will happen in plain English and tell the user to use the Confirm button on the pending action card.
                  Never show raw JSON, proposalId, expiresAt, or tool results to the user.
                  Use get_active_alerts or get_alerts first to find alert IDs before proposing acknowledge or resolve.
                  """
                : """
                  The current user role is read-only. You cannot propose or execute write actions.
                  If asked to acknowledge alerts, resolve alerts, create devices, or control actuators, explain that an Admin or Operator must do that.
                  """;

            var ragInstructions = ragEnabled
                ? """
                  For setup, deployment, MQTT topics, troubleshooting, or how-to questions, call search_documentation before answering.
                  When you use documentation, cite the source file name and section heading in your answer (for example: "Source: Documents/010_UserManual.md — Device Configuration").
                  If search_documentation returns no results, say the topic is not covered in the indexed project docs.
                  """
                : string.Empty;

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
                        For actuator on/off status, call get_actuators_by_device and report LastKnownState, parsedIsOn, and stateDescription.
                        Discrete actuators use on/off (LastKnownState may also be "1" or "0"); analog actuators report their last numeric value.
                        Actuator commands do not require sensor threshold checks — that applies only to alert rules.
                        Never output raw tool-call JSON, function names, or API syntax to the user.
                        When calling tools that need deviceId, use the numeric ID from get_devices, not the device name.
                        {writeInstructions}
                        {ragInstructions}
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

        private static string BuildProposalUserReply(AgentActionProposalDto proposal) =>
            $"I've prepared a pending action for your review:\n\n{proposal.Summary}\n\n" +
            "Use the Confirm button on the pending action card to execute it, or Cancel to dismiss it. " +
            "Nothing will run until you confirm.";

        private static string GetRole(ClaimsPrincipal user) =>
            user.FindFirst("role")?.Value
            ?? user.FindFirst(ClaimTypes.Role)?.Value
            ?? "Viewer";
    }
}

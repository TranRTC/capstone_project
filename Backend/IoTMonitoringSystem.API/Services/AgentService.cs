using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentService : IAgentService
    {
        private const int MaxToolIterations = 8;

        private readonly ILlmClient _llmClient;
        private readonly AgentToolExecutor _toolExecutor;
        private readonly IAgentActionService _agentActionService;
        private readonly AgentToggleIntentHandler _toggleIntentHandler;
        private readonly AgentIntentRouter _intentRouter;
        private readonly IAgentAuditService _auditService;
        private readonly IAgentChatSessionService _sessionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;

        public AgentService(
            ILlmClient llmClient,
            AgentToolExecutor toolExecutor,
            IAgentActionService agentActionService,
            AgentToggleIntentHandler toggleIntentHandler,
            AgentIntentRouter intentRouter,
            IAgentAuditService auditService,
            IAgentChatSessionService sessionService,
            IConfiguration configuration,
            ILogger<AgentService> logger)
        {
            _llmClient = llmClient;
            _toolExecutor = toolExecutor;
            _agentActionService = agentActionService;
            _toggleIntentHandler = toggleIntentHandler;
            _intentRouter = intentRouter;
            _auditService = auditService;
            _sessionService = sessionService;
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

            var sw = Stopwatch.StartNew();
            var username = GetUsername(user) ?? "unknown";
            var role = GetRole(user);
            var writeActionsEnabled = _configuration.GetValue("Agent:Actions:Enabled", true);
            var canWrite = writeActionsEnabled && role is "Admin" or "Operator";
            var ragEnabled = _configuration.GetValue("Agent:Rag:Enabled", true);
            var context = request.Context;
            var session = await _sessionService.GetOrCreateSessionAsync(username, request.SessionId, context);

            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                var directActuator = await TryDirectActuatorCommandResponseAsync(request.Message, user, cancellationToken);
                if (directActuator is not null)
                    return await FinalizeResponseAsync(request, username, role, session, directActuator, sw, context, usedIntentRouter: false);
            }

            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                return await FinalizeResponseAsync(request, username, role, session, new AgentChatResponse
                {
                    Reply = "I could not match the device and actuator. Try: \"Turn off actuator 1 on device 1\"."
                }, sw, context, usedIntentRouter: false);
            }

            var intent = await _intentRouter.TryRouteAsync(request.Message, context, cancellationToken);
            if (intent is { Handled: true })
            {
                foreach (var tool in intent.ToolsUsed)
                    await _auditService.LogToolCallAsync(username, role, tool, true, sessionId: session.AgentChatSessionId);

                return await FinalizeResponseAsync(request, username, role, session, new AgentChatResponse
                {
                    Reply = intent.Reply,
                    ToolsUsed = intent.ToolsUsed,
                    DataAsOfUtc = intent.DataAsOfUtc,
                    UsedIntentRouter = true
                }, sw, context, usedIntentRouter: true);
            }

            var maxHistory = _configuration.GetValue("Agent:MaxHistoryMessages", 10);
            var messages = BuildMessages(request, role, canWrite, ragEnabled, maxHistory, context);
            var tools = BuildTools(canWrite, ragEnabled);
            var toolsUsed = new List<string>();
            var docSourcesUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AgentActionProposalDto? pendingAction = null;
            var dataAsOfUtc = DateTime.UtcNow;

            var knownToolNames = tools.Select(t => t.Name).ToList();

            for (var iteration = 0; iteration < MaxToolIterations; iteration++)
            {
                LlmChatResult result;
                try
                {
                    result = await _llmClient.CompleteAsync(messages, tools, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM completion failed");
                    await _auditService.LogAsync("LlmError", username, role, summary: ex.Message, sessionId: session.AgentChatSessionId, success: false);
                    throw;
                }

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

                    return await FinalizeResponseAsync(request, username, role, session, new AgentChatResponse
                    {
                        Reply = reply,
                        ToolsUsed = toolsUsed.Distinct().ToList(),
                        DocSourcesUsed = docSourcesUsed.ToList(),
                        PendingAction = pendingAction,
                        DataAsOfUtc = dataAsOfUtc
                    }, sw, context, usedIntentRouter: false);
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
                    var toolSw = Stopwatch.StartNew();

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
                            await _auditService.LogAsync("ActionProposed", username, role, toolCall.Name, pendingAction.Summary,
                                sessionId: session.AgentChatSessionId, relatedDeviceId: pendingAction.RelatedDeviceId,
                                relatedAlertId: pendingAction.RelatedAlertId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to create action proposal from tool {Tool}", toolCall.Name);
                            toolResult = JsonSerializer.Serialize(new { success = false, error = ex.Message });
                            await _auditService.LogToolCallAsync(username, role, toolCall.Name, false, (int)toolSw.ElapsedMilliseconds, session.AgentChatSessionId);
                        }
                    }
                    else
                    {
                        toolResult = await _toolExecutor.ExecuteAsync(toolCall.Name, toolCall.ArgumentsJson);
                        dataAsOfUtc = TryExtractDataAsOf(toolResult) ?? dataAsOfUtc;
                        if (toolCall.Name == "search_documentation")
                            CollectDocSources(toolResult, docSourcesUsed);
                        await _auditService.LogToolCallAsync(username, role, toolCall.Name, true, (int)toolSw.ElapsedMilliseconds, session.AgentChatSessionId);
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
                    return await FinalizeResponseAsync(request, username, role, session, new AgentChatResponse
                    {
                        Reply = BuildProposalUserReply(pendingAction),
                        ToolsUsed = toolsUsed.Distinct().ToList(),
                        DocSourcesUsed = docSourcesUsed.ToList(),
                        PendingAction = pendingAction,
                        DataAsOfUtc = dataAsOfUtc
                    }, sw, context, usedIntentRouter: false);
                }
            }

            _logger.LogWarning("Agent tool loop exceeded {Max} iterations", MaxToolIterations);
            await _auditService.LogAsync("LoopLimit", username, role, summary: request.Message, sessionId: session.AgentChatSessionId);

            if (canWrite && AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message))
            {
                var directToggle = await TryDirectActuatorCommandResponseAsync(request.Message, user, cancellationToken);
                if (directToggle is not null)
                    return await FinalizeResponseAsync(request, username, role, session, directToggle, sw, context, usedIntentRouter: false);
            }

            return await FinalizeResponseAsync(request, username, role, session, new AgentChatResponse
            {
                Reply = AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(request.Message)
                    ? "I could not match the device and actuator. Try: \"Turn on actuator 1 on device 1\" or \"Toggle actuator Cyliner on device 1\"."
                    : "I need too many data lookups to answer that question. Please try a simpler request.",
                ToolsUsed = toolsUsed.Distinct().ToList(),
                DocSourcesUsed = docSourcesUsed.ToList(),
                PendingAction = pendingAction,
                DataAsOfUtc = dataAsOfUtc
            }, sw, context, usedIntentRouter: false);
        }

        private async Task<AgentChatResponse> FinalizeResponseAsync(
            AgentChatRequest request,
            string username,
            string role,
            AgentChatSession session,
            AgentChatResponse response,
            Stopwatch sw,
            AgentChatContextDto? context,
            bool usedIntentRouter)
        {
            var tools = response.ToolsUsed?.Distinct().ToList() ?? new List<string>();
            var dataAsOf = response.DataAsOfUtc ?? DateTime.UtcNow;
            var reply = AppendCitationFooter(response.Reply, tools, dataAsOf, usedIntentRouter);

            var finalized = new AgentChatResponse
            {
                Reply = reply,
                ToolsUsed = tools,
                DocSourcesUsed = response.DocSourcesUsed ?? new List<string>(),
                PendingAction = response.PendingAction,
                SessionId = session.AgentChatSessionId,
                DataAsOfUtc = dataAsOf,
                ContextUsed = context,
                UsedIntentRouter = usedIntentRouter
            };

            if (_configuration.GetValue("Agent:Sessions:Enabled", true))
            {
                await _sessionService.AppendMessagesAsync(
                    session.AgentChatSessionId,
                    username,
                    request.Message,
                    reply,
                    tools,
                    dataAsOf);
            }

            await _auditService.LogAsync(
                "Chat",
                username,
                role,
                summary: request.Message.Length > 200 ? request.Message[..200] : request.Message,
                details: new { tools, usedIntentRouter, context },
                relatedDeviceId: context?.DeviceId,
                relatedAlertId: context?.AlertId,
                sessionId: session.AgentChatSessionId,
                durationMs: (int)sw.ElapsedMilliseconds);

            return finalized;
        }

        private static string AppendCitationFooter(string reply, IReadOnlyList<string> tools, DateTime dataAsOfUtc, bool usedIntentRouter)
        {
            if (tools.Count == 0 && !usedIntentRouter)
                return reply;

            var footer = $"\n\n— Live data as of {dataAsOfUtc:yyyy-MM-dd HH:mm:ss} UTC";
            if (tools.Count > 0)
                footer += $" · Tools: {string.Join(", ", tools)}";
            return reply.TrimEnd() + footer;
        }

        private static DateTime? TryExtractDataAsOf(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("dataAsOfUtc", out var prop) && prop.TryGetDateTime(out var dt))
                    return dt;
            }
            catch { }
            return null;
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
            catch { }
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
            int maxHistory,
            AgentChatContextDto? context)
        {
            var contextLine = BuildContextLine(context);
            var writeInstructions = canWrite
                ? """
                  You may propose write actions using propose_acknowledge_alert, propose_resolve_alert, propose_create_device, propose_send_device_command, or propose_toggle_actuator.
                  Prefer get_alert_summary and get_operational_snapshot for overview questions instead of many separate tool calls.
                  Use find_devices when the user refers to a device by name.
                  When the user asks to toggle an actuator: call get_actuators_by_device, then call propose_toggle_actuator with deviceId and actuatorId.
                  Never ask "is this correct?" for actuator commands.
                  Only one pending action is allowed per user.
                  Never claim an action was executed until the user confirms it in the dashboard.
                  Use get_active_alerts or get_alerts first to find alert IDs before proposing acknowledge or resolve.
                  """
                : """
                  The current user role is read-only. You cannot propose or execute write actions.
                  """;

            var ragInstructions = ragEnabled
                ? "For setup or troubleshooting, call search_documentation and cite the source file."
                : string.Empty;

            var messages = new List<LlmMessage>
            {
                new()
                {
                    Role = "system",
                    Content = $"""
                        You are a professional IoT monitoring assistant for operators and engineers.
                        User role: {role}.
                        {contextLine}
                        Use tools for all factual answers. Never invent device, sensor, or alert data.
                        Prefer summary tools (get_alert_summary, get_operational_snapshot, get_sensor_reading_summary) over raw dumps.
                        Keep answers concise with IDs and status fields.
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

        private static string BuildContextLine(AgentChatContextDto? context)
        {
            if (context is null)
                return string.Empty;

            var parts = new List<string>();
            if (context.DeviceId is int deviceId)
                parts.Add($"The user is viewing device ID {deviceId}. Prefer this device when they say \"this device\" or omit a device ID.");
            if (context.AlertId is long alertId)
                parts.Add($"Focused alert ID: {alertId}.");
            if (!string.IsNullOrWhiteSpace(context.Route))
                parts.Add($"Current page route: {context.Route}.");

            return parts.Count == 0 ? string.Empty : "Context: " + string.Join(" ", parts);
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

        private static string? GetUsername(ClaimsPrincipal user) =>
            user.Identity?.Name
            ?? user.FindFirst("unique_name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value;
    }
}

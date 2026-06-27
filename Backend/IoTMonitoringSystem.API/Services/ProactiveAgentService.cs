using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Repositories;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    public interface IProactiveAgentService
    {
        Task<AgentProactiveStatusDto> GetProactiveStatusAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<AgentInsightDto>> GetInsightsAsync(AgentInsightQueryDto query, CancellationToken cancellationToken = default);
        Task<AgentInsightDto> DismissInsightAsync(long id, CancellationToken cancellationToken = default);
        Task<AgentOpenInChatResponse> GetOpenInChatSeedAsync(long id, CancellationToken cancellationToken = default);
        Task HandleNewAlertAsync(AlertDto alert, CancellationToken cancellationToken = default);
        Task RunMonitoringSweepAsync(CancellationToken cancellationToken = default);
    }

    public class ProactiveAgentService : IProactiveAgentService
    {
        private static DateTime? _lastSweepAtUtc;

        private readonly IAgentInsightRepository _insightRepository;
        private readonly IDeviceService _deviceService;
        private readonly ILlmClient _llmClient;
        private readonly AgentToolExecutor _toolExecutor;
        private readonly AgentTriggerGate _triggerGate;
        private readonly INotificationService _notificationService;
        private readonly IMqttRuntimeState _mqttRuntimeState;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProactiveAgentService> _logger;

        public ProactiveAgentService(
            IAgentInsightRepository insightRepository,
            IDeviceService deviceService,
            ILlmClient llmClient,
            AgentToolExecutor toolExecutor,
            AgentTriggerGate triggerGate,
            INotificationService notificationService,
            IMqttRuntimeState mqttRuntimeState,
            IConfiguration configuration,
            ILogger<ProactiveAgentService> logger)
        {
            _insightRepository = insightRepository;
            _deviceService = deviceService;
            _llmClient = llmClient;
            _toolExecutor = toolExecutor;
            _triggerGate = triggerGate;
            _notificationService = notificationService;
            _mqttRuntimeState = mqttRuntimeState;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsEnabled => _configuration.GetValue("Agent:Proactive:Enabled", true);

        public async Task<AgentProactiveStatusDto> GetProactiveStatusAsync(CancellationToken cancellationToken = default)
        {
            var activeCount = await _insightRepository.CountAsync("Active");
            return new AgentProactiveStatusDto
            {
                Enabled = IsEnabled,
                LlmConfigured = _llmClient.IsConfigured,
                ActiveInsightCount = activeCount,
                LastSweepAtUtc = _lastSweepAtUtc
            };
        }

        public async Task<PagedResult<AgentInsightDto>> GetInsightsAsync(
            AgentInsightQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var items = await _insightRepository.GetInsightsAsync(query.Status, query.PageNumber, query.PageSize);
            var total = await _insightRepository.CountAsync(query.Status);
            return new PagedResult<AgentInsightDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<AgentInsightDto> DismissInsightAsync(long id, CancellationToken cancellationToken = default)
        {
            var insight = await _insightRepository.DismissAsync(id);
            return MapToDto(insight);
        }

        public async Task<AgentOpenInChatResponse> GetOpenInChatSeedAsync(long id, CancellationToken cancellationToken = default)
        {
            var insight = await _insightRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Agent insight {id} not found.");

            return new AgentOpenInChatResponse
            {
                SeedMessage =
                    $"Tell me more about this proactive insight: {insight.Title}. Context: {insight.Summary}"
            };
        }

        public async Task HandleNewAlertAsync(AlertDto alert, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
                return;

            var dedupeKey = $"NewAlert:{alert.DeviceId}:{alert.AlertRuleId}";
            if (!await _triggerGate.ShouldProcessAsync(dedupeKey, cancellationToken))
                return;

            string deviceName = $"Device {alert.DeviceId}";
            try
            {
                var device = await _deviceService.GetDeviceByIdAsync(alert.DeviceId);
                deviceName = device.DeviceName;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not load device {DeviceId} for proactive alert insight", alert.DeviceId);
            }

            var severity = NormalizeSeverity(alert.Severity);
            var fallbackTitle = $"{alert.Severity} alert on {deviceName}";
            var fallbackSummary =
                $"{alert.Message}. Trigger value: {alert.TriggerValue}. Review the Alerts page and check recent readings for device {alert.DeviceId}.";

            var contextJson = JsonSerializer.Serialize(new
            {
                trigger = "NewAlert",
                alert = new
                {
                    alert.AlertId,
                    alert.DeviceId,
                    deviceName,
                    alert.Severity,
                    alert.Message,
                    alert.TriggerValue,
                    alert.TriggeredAt
                }
            });

            await CreateInsightAsync(
                triggerType: "NewAlert",
                dedupeKey: dedupeKey,
                severity: severity,
                fallbackTitle: fallbackTitle,
                fallbackSummary: fallbackSummary,
                suggestedActions: new[]
                {
                    "Open the Alerts page to review details.",
                    "Check recent sensor readings for this device.",
                    "Acknowledge the alert once reviewed."
                },
                relatedAlertId: alert.AlertId,
                relatedDeviceId: alert.DeviceId,
                contextJson: contextJson,
                cancellationToken: cancellationToken);
        }

        public async Task RunMonitoringSweepAsync(CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
                return;

            _lastSweepAtUtc = DateTime.UtcNow;

            await CheckOfflineDevicesAsync(cancellationToken);
            await CheckMqttHealthAsync(cancellationToken);
        }

        private async Task CheckOfflineDevicesAsync(CancellationToken cancellationToken)
        {
            var offlineMinutes = _configuration.GetValue("Agent:Proactive:DeviceOfflineMinutes", 15);
            var cutoff = DateTime.UtcNow.AddMinutes(-offlineMinutes);
            var devices = await _deviceService.GetAllDevicesAsync();

            foreach (var device in devices.Where(d => d.IsActive))
            {
                if (device.LastSeenAt.HasValue && device.LastSeenAt.Value >= cutoff)
                    continue;

                var dedupeKey = $"DeviceOffline:{device.DeviceId}";
                if (!await _triggerGate.ShouldProcessAsync(dedupeKey, cancellationToken))
                    continue;

                var lastSeenText = device.LastSeenAt?.ToString("u") ?? "never";
                var fallbackTitle = $"Device offline: {device.DeviceName}";
                var fallbackSummary =
                    $"{device.DeviceName} (ID {device.DeviceId}) has not reported readings since {lastSeenText}. Check MQTT connectivity and edge firmware.";

                var contextJson = JsonSerializer.Serialize(new
                {
                    trigger = "DeviceOffline",
                    device = new
                    {
                        device.DeviceId,
                        device.DeviceName,
                        device.LastSeenAt,
                        offlineMinutes
                    }
                });

                await CreateInsightAsync(
                    triggerType: "DeviceOffline",
                    dedupeKey: dedupeKey,
                    severity: "warning",
                    fallbackTitle: fallbackTitle,
                    fallbackSummary: fallbackSummary,
                    suggestedActions: new[]
                    {
                        "Verify the edge device is powered and connected.",
                        "Check MQTT broker and device topic configuration.",
                        "Review system health for pipeline status."
                    },
                    relatedAlertId: null,
                    relatedDeviceId: device.DeviceId,
                    contextJson: contextJson,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task CheckMqttHealthAsync(CancellationToken cancellationToken)
        {
            if (_mqttRuntimeState.IsConnected)
                return;

            var unhealthyMinutes = _configuration.GetValue("Agent:Proactive:MqttUnhealthyMinutes", 5);
            var dedupeKey = "MqttUnhealthy";
            if (!await _triggerGate.ShouldProcessAsync(dedupeKey, cancellationToken))
                return;

            var fallbackTitle = "MQTT pipeline disconnected";
            var fallbackSummary =
                "The API is not connected to the MQTT broker. Live readings and commands may be affected until the connection is restored.";
            if (_mqttRuntimeState.LastError is not null)
                fallbackSummary += $" Last error: {_mqttRuntimeState.LastError}";

            var contextJson = JsonSerializer.Serialize(new
            {
                trigger = "MqttUnhealthy",
                mqtt = new
                {
                    _mqttRuntimeState.IsConnected,
                    _mqttRuntimeState.IsSubscribed,
                    _mqttRuntimeState.LastMessageReceivedAtUtc,
                    _mqttRuntimeState.LastError,
                    unhealthyMinutes
                }
            });

            await CreateInsightAsync(
                triggerType: "MqttUnhealthy",
                dedupeKey: dedupeKey,
                severity: "critical",
                fallbackTitle: fallbackTitle,
                fallbackSummary: fallbackSummary,
                suggestedActions: new[]
                {
                    "Check whether the MQTT broker is running.",
                    "Review MQTT host/port settings for this environment.",
                    "Open System Health or the Assistant for pipeline details."
                },
                relatedAlertId: null,
                relatedDeviceId: null,
                contextJson: contextJson,
                cancellationToken: cancellationToken);
        }

        private async Task CreateInsightAsync(
            string triggerType,
            string dedupeKey,
            string severity,
            string fallbackTitle,
            string fallbackSummary,
            IReadOnlyList<string> suggestedActions,
            long? relatedAlertId,
            int? relatedDeviceId,
            string contextJson,
            CancellationToken cancellationToken)
        {
            string title = fallbackTitle;
            string summary = fallbackSummary;
            var toolsUsed = new List<string>();
            var usedLlm = false;

            if (_llmClient.IsConfigured)
            {
                try
                {
                    var llmResult = await GenerateLlmSummaryAsync(contextJson, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(llmResult.Summary))
                    {
                        summary = llmResult.Summary.Trim();
                        if (!string.IsNullOrWhiteSpace(llmResult.Title))
                            title = llmResult.Title.Trim();
                        toolsUsed = llmResult.ToolsUsed;
                        usedLlm = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Proactive LLM summary failed for {TriggerType}; using fallback", triggerType);
                }
            }

            var insight = new AgentInsight
            {
                TriggerType = triggerType,
                DedupeKey = dedupeKey,
                Severity = severity,
                Title = title,
                Summary = summary,
                SuggestedActionsJson = JsonSerializer.Serialize(suggestedActions),
                RelatedAlertId = relatedAlertId,
                RelatedDeviceId = relatedDeviceId,
                ToolsUsedJson = toolsUsed.Count > 0 ? JsonSerializer.Serialize(toolsUsed) : null,
                Status = "Active",
                UsedLlm = usedLlm,
                CreatedAt = DateTime.UtcNow
            };

            insight = await _insightRepository.CreateAsync(insight);
            var dto = MapToDto(insight);
            await _notificationService.NotifyAgentInsightAsync(dto);

            _logger.LogInformation(
                "Created proactive insight {InsightId} ({TriggerType}, LLM={UsedLlm})",
                insight.AgentInsightId,
                triggerType,
                usedLlm);
        }

        private async Task<(string? Title, string Summary, List<string> ToolsUsed)> GenerateLlmSummaryAsync(
            string contextJson,
            CancellationToken cancellationToken)
        {
            var tools = AgentToolDefinitions.BuildReadOnlyTools();
            var knownToolNames = tools.Select(t => t.Name).ToList();
            var toolsUsed = new List<string>();

            var messages = new List<LlmMessage>
            {
                new()
                {
                    Role = "system",
                    Content = """
                        You generate short operator notifications for an IoT monitoring dashboard.
                        Respond in plain English only. Never output JSON, tool names, or function syntax.
                        Format exactly as:
                        TITLE: one short headline
                        SUMMARY: 2-4 sentences explaining what happened and why it matters
                        You may use one round of read-only tools if needed for recent readings or device context.
                        """
                },
                new()
                {
                    Role = "user",
                    Content = $"Generate an operator notification for this event context:\n{contextJson}"
                }
            };

            for (var i = 0; i < 2; i++)
            {
                var result = await _llmClient.CompleteAsync(messages, tools, cancellationToken);
                var toolCalls = result.HasToolCalls
                    ? result.ToolCalls
                    : AgentToolCallParser.TryParseFromContent(result.Content, knownToolNames);

                if (toolCalls.Count == 0)
                {
                    return (ParseTitle(result.Content), ParseSummary(result.Content), toolsUsed);
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

            return (null, string.Empty, toolsUsed);
        }

        private static string? ParseTitle(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                    return line["TITLE:".Length..].Trim();
            }

            return null;
        }

        private static string ParseSummary(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                    return string.Join(' ', lines.Skip(i).Select(l =>
                        l.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase)
                            ? l["SUMMARY:".Length..].Trim()
                            : l));
            }

            return content.Trim();
        }

        private static string NormalizeSeverity(string? severity) =>
            severity?.ToLowerInvariant() switch
            {
                "critical" => "critical",
                "high" => "warning",
                "medium" => "info",
                _ => "info"
            };

        private static AgentInsightDto MapToDto(AgentInsight insight)
        {
            var actions = new List<string>();
            if (!string.IsNullOrWhiteSpace(insight.SuggestedActionsJson))
            {
                try
                {
                    actions = JsonSerializer.Deserialize<List<string>>(insight.SuggestedActionsJson) ?? new List<string>();
                }
                catch
                {
                    actions = new List<string>();
                }
            }

            return new AgentInsightDto
            {
                AgentInsightId = insight.AgentInsightId,
                TriggerType = insight.TriggerType,
                Severity = insight.Severity,
                Title = insight.Title,
                Summary = insight.Summary,
                SuggestedActions = actions,
                RelatedAlertId = insight.RelatedAlertId,
                RelatedDeviceId = insight.RelatedDeviceId,
                Status = insight.Status,
                UsedLlm = insight.UsedLlm,
                CreatedAt = insight.CreatedAt,
                DismissedAt = insight.DismissedAt,
                ChatSeedMessage =
                    $"Tell me more about this insight: {insight.Title}. {insight.Summary}"
            };
        }
    }
}

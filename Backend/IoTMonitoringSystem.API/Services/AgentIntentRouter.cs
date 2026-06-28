using System.Text.RegularExpressions;
using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentIntentRouter
    {
        private readonly IIotAgentToolService _tools;
        private readonly IConfiguration _configuration;

        public AgentIntentRouter(IIotAgentToolService tools, IConfiguration configuration)
        {
            _tools = tools;
            _configuration = configuration;
        }

        public bool IsEnabled => _configuration.GetValue("Agent:IntentRouter:Enabled", true);

        public async Task<AgentIntentResult?> TryRouteAsync(
            string message,
            AgentChatContextDto? context,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
                return null;

            // Actuator commands (including typos like "turn of") go to the direct handler, not data lookup.
            if (AgentToggleIntentHandler.LooksLikeActuatorCommandRequest(message))
                return null;

            var text = message.Trim();
            var lower = text.ToLowerInvariant();
            var deviceId = context?.DeviceId ?? TryExtractDeviceId(text);

            if (MatchesAny(lower, "mqtt", "system health", "pipeline health", "is mqtt"))
            {
                var json = await _tools.GetOperationalSnapshotJsonAsync(deviceId, cancellationToken);
                return Handled("get_operational_snapshot", json);
            }

            if (MatchesAny(lower, "offline device", "devices offline", "any offline"))
            {
                var json = await _tools.GetOperationalSnapshotJsonAsync(null, cancellationToken);
                return Handled("get_operational_snapshot", json);
            }

            if (MatchesAny(lower, "operational snapshot", "system status", "overall status", "what's wrong", "whats wrong"))
            {
                var json = await _tools.GetOperationalSnapshotJsonAsync(deviceId, cancellationToken);
                return Handled("get_operational_snapshot", json);
            }

            if (MatchesAny(lower, "alert summary", "active alerts", "critical alerts", "any alerts", "show alerts"))
            {
                var json = await _tools.GetAlertSummaryJsonAsync(deviceId, cancellationToken);
                return Handled("get_alert_summary", json);
            }

            if (MatchesAny(lower, "list devices", "all devices", "show devices", "registered devices"))
            {
                var json = await _tools.FindDevicesJsonAsync(string.Empty, cancellationToken);
                return Handled("find_devices", json);
            }

            if (deviceId.HasValue && MatchesAny(lower, "sensor summary", "reading summary", "sensor readings", "temperature", "readings"))
            {
                var json = await _tools.GetSensorReadingSummaryJsonAsync(deviceId.Value, 24, null, cancellationToken);
                return Handled("get_sensor_reading_summary", json);
            }

            if (deviceId.HasValue && MatchesAny(lower, "actuator", "actuators"))
            {
                var json = await _tools.GetActuatorsByDeviceJsonAsync(deviceId.Value, cancellationToken);
                return Handled("get_actuators_by_device", json);
            }

            if (deviceId.HasValue && MatchesAny(lower, "sensors on", "device sensors", "this device"))
            {
                var json = await _tools.GetOperationalSnapshotJsonAsync(deviceId, cancellationToken);
                return Handled("get_operational_snapshot", json);
            }

            var findMatch = Regex.Match(text, @"find device(?:s)?\s+(.+)$", RegexOptions.IgnoreCase);
            if (findMatch.Success)
            {
                var json = await _tools.FindDevicesJsonAsync(findMatch.Groups[1].Value.Trim(), cancellationToken);
                return Handled("find_devices", json);
            }

            return null;
        }

        private static AgentIntentResult Handled(string toolName, string json)
        {
            var dataAsOf = DateTime.UtcNow;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("dataAsOfUtc", out var prop) &&
                    prop.TryGetDateTime(out var parsed))
                    dataAsOf = parsed;
            }
            catch
            {
                // keep UtcNow
            }

            return new AgentIntentResult
            {
                Handled = true,
                Reply = AgentToolReplyFormatter.Format(toolName, json),
                ToolsUsed = new List<string> { toolName },
                DataAsOfUtc = dataAsOf
            };
        }

        private static bool MatchesAny(string lower, params string[] phrases) =>
            phrases.Any(p => lower.Contains(p, StringComparison.Ordinal));

        private static int? TryExtractDeviceId(string text)
        {
            var match = Regex.Match(text, @"\bdevice\s*#?\s*(\d+)\b", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }
    }

    public class AgentIntentResult
    {
        public bool Handled { get; init; }
        public string Reply { get; init; } = string.Empty;
        public List<string> ToolsUsed { get; init; } = new();
        public DateTime DataAsOfUtc { get; init; } = DateTime.UtcNow;
    }
}

using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentToolExecutor
    {
        private const int MaxReadingsReturned = 50;

        private readonly IDeviceService _deviceService;
        private readonly IAlertService _alertService;
        private readonly ISensorService _sensorService;
        private readonly ISensorReadingService _sensorReadingService;
        private readonly IActuatorService _actuatorService;
        private readonly IMqttRuntimeState _mqttRuntimeState;
        private readonly IMqttIngestMetrics _mqttIngestMetrics;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentToolExecutor> _logger;

        public AgentToolExecutor(
            IDeviceService deviceService,
            IAlertService alertService,
            ISensorService sensorService,
            ISensorReadingService sensorReadingService,
            IActuatorService actuatorService,
            IMqttRuntimeState mqttRuntimeState,
            IMqttIngestMetrics mqttIngestMetrics,
            IConfiguration configuration,
            ILogger<AgentToolExecutor> logger)
        {
            _deviceService = deviceService;
            _alertService = alertService;
            _sensorService = sensorService;
            _sensorReadingService = sensorReadingService;
            _actuatorService = actuatorService;
            _mqttRuntimeState = mqttRuntimeState;
            _mqttIngestMetrics = mqttIngestMetrics;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> ExecuteAsync(string toolName, string argumentsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
                var root = doc.RootElement;

                return toolName switch
                {
                    "get_devices" => JsonSerializer.Serialize(await _deviceService.GetAllDevicesAsync()),
                    "get_device" => await GetDeviceAsync(root),
                    "get_active_alerts" => JsonSerializer.Serialize(await _alertService.GetActiveAlertsAsync()),
                    "get_alerts" => await GetAlertsAsync(root),
                    "get_sensors_by_device" => await GetSensorsByDeviceAsync(root),
                    "get_actuators_by_device" => await GetActuatorsByDeviceAsync(root),
                    "get_recent_readings" => await GetRecentReadingsAsync(root),
                    "get_system_health" => JsonSerializer.Serialize(GetSystemHealth()),
                    _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{toolName}'." })
                };
            }
            catch (KeyNotFoundException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent tool {ToolName} failed", toolName);
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        private async Task<string> GetDeviceAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var device = await _deviceService.GetDeviceByIdAsync(deviceId);
            return JsonSerializer.Serialize(device);
        }

        private async Task<string> GetAlertsAsync(JsonElement root)
        {
            var query = new AlertQueryDto
            {
                DeviceId = TryGetInt(root, "deviceId"),
                Status = TryGetString(root, "status"),
                Severity = TryGetString(root, "severity"),
                PageNumber = 1,
                PageSize = 50
            };
            var result = await _alertService.GetAlertsAsync(query);
            return JsonSerializer.Serialize(result);
        }

        private async Task<string> GetSensorsByDeviceAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var sensors = await _sensorService.GetSensorsByDeviceIdAsync(deviceId);
            return JsonSerializer.Serialize(sensors);
        }

        private async Task<string> GetActuatorsByDeviceAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            return JsonSerializer.Serialize(actuators);
        }

        private async Task<string> GetRecentReadingsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var hours = Math.Clamp(TryGetInt(root, "hours") ?? 24, 1, 168);
            var end = DateTime.UtcNow;
            var start = end.AddHours(-hours);
            var readings = await _sensorReadingService.GetReadingsByDeviceIdAsync(deviceId, start, end);
            if (readings.Count > MaxReadingsReturned)
                readings = readings.OrderByDescending(r => r.Timestamp).Take(MaxReadingsReturned).ToList();
            return JsonSerializer.Serialize(new { hours, count = readings.Count, readings });
        }

        private object GetSystemHealth()
        {
            return new
            {
                api = new { status = "healthy", timestamp = DateTime.UtcNow },
                mqtt = new
                {
                    host = _configuration.GetValue<string>("Mqtt:Host", "localhost"),
                    port = _configuration.GetValue<int>("Mqtt:Port", 1883),
                    isConnected = _mqttRuntimeState.IsConnected,
                    isSubscribed = _mqttRuntimeState.IsSubscribed,
                    lastMessageReceivedAtUtc = _mqttRuntimeState.LastMessageReceivedAtUtc,
                    totalMessagesReceived = _mqttIngestMetrics.TotalMessagesReceived,
                    lastError = _mqttRuntimeState.LastError
                }
            };
        }

        private async Task<int> ResolveDeviceIdAsync(JsonElement root)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (!prop.Name.Equals("deviceId", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var numericId))
                    return numericId;

                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var raw = prop.Value.GetString();
                    if (int.TryParse(raw, out var parsedId))
                        return parsedId;

                    return await LookupDeviceIdByNameAsync(raw);
                }
            }

            throw new ArgumentException("Missing or invalid 'deviceId' argument.");
        }

        private async Task<int> LookupDeviceIdByNameAsync(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Missing or invalid 'deviceId' argument.");

            var devices = await _deviceService.GetAllDevicesAsync();
            var match = devices.FirstOrDefault(d =>
                d.DeviceName.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? devices.FirstOrDefault(d =>
                    d.DeviceName.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new KeyNotFoundException($"Device not found: {name}");

            return match.DeviceId;
        }

        private static int GetRequiredInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop) || !prop.TryGetInt32(out var value))
                throw new ArgumentException($"Missing or invalid '{name}' argument.");
            return value;
        }

        private static int? TryGetInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop))
                return null;
            return prop.TryGetInt32(out var value) ? value : null;
        }

        private static string? TryGetString(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var prop))
                return null;
            return prop.GetString();
        }
    }
}

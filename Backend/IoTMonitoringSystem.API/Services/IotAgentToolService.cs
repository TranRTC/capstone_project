using System.Text.Json;
using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Services;

namespace IoTMonitoringSystem.API.Services
{
    public class IotAgentToolService : IIotAgentToolService
    {
        private const int MaxReadingsReturned = 50;

        private readonly IDeviceService _deviceService;
        private readonly IAlertService _alertService;
        private readonly ISensorService _sensorService;
        private readonly ISensorReadingService _sensorReadingService;
        private readonly IActuatorService _actuatorService;
        private readonly IMqttRuntimeState _mqttRuntimeState;
        private readonly IMqttIngestMetrics _mqttIngestMetrics;
        private readonly IDocumentationSearchService? _documentationSearch;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IotAgentToolService> _logger;

        public IotAgentToolService(
            IDeviceService deviceService,
            IAlertService alertService,
            ISensorService sensorService,
            ISensorReadingService sensorReadingService,
            IActuatorService actuatorService,
            IMqttRuntimeState mqttRuntimeState,
            IMqttIngestMetrics mqttIngestMetrics,
            IConfiguration configuration,
            ILogger<IotAgentToolService> logger,
            IDocumentationSearchService? documentationSearch = null)
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
            _documentationSearch = documentationSearch;
        }

        public async Task<string> ExecuteAsync(string toolName, string argumentsJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
                var root = doc.RootElement;

                return toolName switch
                {
                    "get_devices" => await GetDevicesJsonAsync(),
                    "get_device" => await GetDeviceJsonFromArgsAsync(root),
                    "get_active_alerts" => await GetActiveAlertsJsonAsync(),
                    "get_alerts" => await GetAlertsJsonFromArgsAsync(root),
                    "get_sensors_by_device" => await GetSensorsByDeviceJsonFromArgsAsync(root),
                    "get_actuators_by_device" => await GetActuatorsByDeviceJsonFromArgsAsync(root),
                    "get_recent_readings" => await GetRecentReadingsJsonFromArgsAsync(root),
                    "get_system_health" => await GetSystemHealthJsonAsync(),
                    "search_documentation" => await SearchDocumentationFromArgsAsync(root),
                    _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{toolName}'." })
                };
            }
            catch (KeyNotFoundException ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IoT agent tool {ToolName} failed", toolName);
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> GetDevicesJsonAsync(CancellationToken cancellationToken = default) =>
            JsonSerializer.Serialize(await _deviceService.GetAllDevicesAsync());

        public async Task<string> GetDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var device = await _deviceService.GetDeviceByIdAsync(deviceId);
            return JsonSerializer.Serialize(device);
        }

        public async Task<string> GetActiveAlertsJsonAsync(CancellationToken cancellationToken = default) =>
            JsonSerializer.Serialize(await _alertService.GetActiveAlertsAsync());

        public async Task<string> GetAlertsJsonAsync(
            int? deviceId,
            string? status,
            string? severity,
            CancellationToken cancellationToken = default)
        {
            var query = new AlertQueryDto
            {
                DeviceId = deviceId,
                Status = status,
                Severity = severity,
                PageNumber = 1,
                PageSize = 50
            };
            var result = await _alertService.GetAlertsAsync(query);
            return JsonSerializer.Serialize(result);
        }

        public async Task<string> GetSensorsByDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var sensors = await _sensorService.GetSensorsByDeviceIdAsync(deviceId);
            return JsonSerializer.Serialize(sensors);
        }

        public async Task<string> GetActuatorsByDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default)
        {
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            var enriched = actuators.Select(a => new
            {
                a.ActuatorId,
                a.DeviceId,
                a.Name,
                a.Description,
                a.Kind,
                a.Channel,
                a.AnalogMin,
                a.AnalogMax,
                a.ControlUnit,
                a.IsActive,
                a.FeedbackSensorId,
                a.LastKnownState,
                a.LastStateAt,
                a.CreatedAt,
                a.UpdatedAt,
                parsedIsOn = ActuatorStateHelper.TryParseIsOn(a.LastKnownState),
                stateDescription = ActuatorStateHelper.DescribeState(a.LastKnownState),
                toggleWouldSetOn = string.Equals(a.Kind, "Discrete", StringComparison.OrdinalIgnoreCase)
                    ? ActuatorStateHelper.ToggleTargetIsOn(a.LastKnownState)
                    : (bool?)null
            });
            return JsonSerializer.Serialize(enriched);
        }

        public async Task<string> GetRecentReadingsJsonAsync(int deviceId, int hours = 24, CancellationToken cancellationToken = default)
        {
            hours = Math.Clamp(hours, 1, 168);
            var end = DateTime.UtcNow;
            var start = end.AddHours(-hours);
            var readings = await _sensorReadingService.GetReadingsByDeviceIdAsync(deviceId, start, end);
            if (readings.Count > MaxReadingsReturned)
                readings = readings.OrderByDescending(r => r.Timestamp).Take(MaxReadingsReturned).ToList();
            return JsonSerializer.Serialize(new { hours, count = readings.Count, readings });
        }

        public Task<string> GetSystemHealthJsonAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(JsonSerializer.Serialize(new
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
            }));

        public Task<string> SearchDocumentationJsonAsync(string query, CancellationToken cancellationToken = default)
        {
            if (_documentationSearch is null || !_documentationSearch.IsEnabled)
                return Task.FromResult(JsonSerializer.Serialize(new { error = "Documentation search is disabled." }));

            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(JsonSerializer.Serialize(new { error = "Missing or invalid 'query' argument." }));

            var results = _documentationSearch.Search(query);
            if (results.Count == 0)
            {
                return Task.FromResult(JsonSerializer.Serialize(new
                {
                    query,
                    count = 0,
                    message = "No matching documentation chunks found.",
                    results = Array.Empty<object>()
                }));
            }

            return Task.FromResult(JsonSerializer.Serialize(new
            {
                query,
                count = results.Count,
                results = results.Select(r => new
                {
                    source = r.SourcePath,
                    heading = r.Heading,
                    excerpt = r.Excerpt,
                    score = r.Score
                })
            }));
        }

        private async Task<string> GetDeviceJsonFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            return await GetDeviceJsonAsync(deviceId);
        }

        private async Task<string> GetAlertsJsonFromArgsAsync(JsonElement root) =>
            await GetAlertsJsonAsync(TryGetInt(root, "deviceId"), TryGetString(root, "status"), TryGetString(root, "severity"));

        private async Task<string> GetSensorsByDeviceJsonFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            return await GetSensorsByDeviceJsonAsync(deviceId);
        }

        private async Task<string> GetActuatorsByDeviceJsonFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            return await GetActuatorsByDeviceJsonAsync(deviceId);
        }

        private async Task<string> GetRecentReadingsJsonFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var hours = Math.Clamp(TryGetInt(root, "hours") ?? 24, 1, 168);
            return await GetRecentReadingsJsonAsync(deviceId, hours);
        }

        private async Task<string> SearchDocumentationFromArgsAsync(JsonElement root)
        {
            var query = TryGetString(root, "query") ?? string.Empty;
            return await SearchDocumentationJsonAsync(query);
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

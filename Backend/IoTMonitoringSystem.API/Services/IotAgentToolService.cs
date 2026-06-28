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
                    "find_devices" => await FindDevicesFromArgsAsync(root),
                    "find_actuators" => await FindActuatorsFromArgsAsync(root),
                    "get_alert_summary" => await GetAlertSummaryFromArgsAsync(root),
                    "get_sensor_reading_summary" => await GetSensorReadingSummaryFromArgsAsync(root),
                    "get_operational_snapshot" => await GetOperationalSnapshotFromArgsAsync(root),
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

        public async Task<string> FindDevicesJsonAsync(string query, CancellationToken cancellationToken = default)
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            if (string.IsNullOrWhiteSpace(query))
                return JsonSerializer.Serialize(new { dataAsOfUtc = DateTime.UtcNow, count = devices.Count, devices });

            var q = query.Trim();
            var matches = devices.Where(d =>
                d.DeviceName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (d.Location?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                d.DeviceType.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                d.DeviceId.ToString() == q).ToList();

            return JsonSerializer.Serialize(new { dataAsOfUtc = DateTime.UtcNow, query = q, count = matches.Count, devices = matches });
        }

        public async Task<string> FindActuatorsJsonAsync(int deviceId, string query, CancellationToken cancellationToken = default)
        {
            var actuators = await _actuatorService.GetByDeviceIdAsync(deviceId);
            if (string.IsNullOrWhiteSpace(query))
                return await GetActuatorsByDeviceJsonAsync(deviceId, cancellationToken);

            var q = query.Trim();
            var matches = actuators.Where(a =>
                a.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.ActuatorId.ToString() == q ||
                (a.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            return JsonSerializer.Serialize(new
            {
                dataAsOfUtc = DateTime.UtcNow,
                deviceId,
                query = q,
                count = matches.Count,
                actuators = matches.Select(a => new
                {
                    a.ActuatorId,
                    a.Name,
                    a.LastKnownState,
                    parsedIsOn = ActuatorStateHelper.TryParseIsOn(a.LastKnownState),
                    stateDescription = ActuatorStateHelper.DescribeState(a.LastKnownState)
                })
            });
        }

        public async Task<string> GetAlertSummaryJsonAsync(int? deviceId = null, CancellationToken cancellationToken = default)
        {
            var active = await _alertService.GetActiveAlertsAsync();
            if (deviceId.HasValue)
                active = active.Where(a => a.DeviceId == deviceId.Value).ToList();

            var bySeverity = active
                .GroupBy(a => a.Severity, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count());

            return JsonSerializer.Serialize(new
            {
                dataAsOfUtc = DateTime.UtcNow,
                deviceId,
                totalActive = active.Count,
                bySeverity,
                recent = active
                    .OrderByDescending(a => a.TriggeredAt)
                    .Take(15)
                    .Select(a => new { a.AlertId, a.DeviceId, a.Severity, a.Status, a.Message, a.TriggeredAt })
            });
        }

        public async Task<string> GetSensorReadingSummaryJsonAsync(int deviceId, int hours = 24, int? sensorId = null, CancellationToken cancellationToken = default)
        {
            hours = Math.Clamp(hours, 1, 168);
            var end = DateTime.UtcNow;
            var start = end.AddHours(-hours);
            var readings = await _sensorReadingService.GetReadingsByDeviceIdAsync(deviceId, start, end);
            if (sensorId.HasValue)
                readings = readings.Where(r => r.SensorId == sensorId.Value).ToList();

            var sensors = await _sensorService.GetSensorsByDeviceIdAsync(deviceId);
            var sensorLookup = sensors.ToDictionary(s => s.SensorId, s => s);

            var summaries = readings
                .GroupBy(r => r.SensorId)
                .Select(g =>
                {
                    var values = g.Select(x => x.Value).ToList();
                    var latest = g.OrderByDescending(x => x.Timestamp).First();
                    sensorLookup.TryGetValue(g.Key, out var sensor);
                    return new
                    {
                        sensorId = g.Key,
                        sensorName = sensor?.SensorName ?? $"Sensor {g.Key}",
                        unit = sensor?.Unit,
                        count = values.Count,
                        min = values.Count > 0 ? values.Min() : (decimal?)null,
                        max = values.Count > 0 ? values.Max() : (decimal?)null,
                        average = values.Count > 0 ? Math.Round(values.Average(), 2) : (decimal?)null,
                        latestValue = latest.Value,
                        latestAt = latest.Timestamp
                    };
                })
                .OrderBy(s => s.sensorName)
                .ToList();

            return JsonSerializer.Serialize(new { dataAsOfUtc = DateTime.UtcNow, deviceId, hours, sensorId, sensors = summaries });
        }

        public async Task<string> GetOperationalSnapshotJsonAsync(int? deviceId = null, CancellationToken cancellationToken = default)
        {
            var offlineMinutes = _configuration.GetValue("Agent:Proactive:DeviceOfflineMinutes", 15);
            var cutoff = DateTime.UtcNow.AddMinutes(-offlineMinutes);
            var devices = await _deviceService.GetAllDevicesAsync();
            var offline = devices
                .Where(d => !d.LastSeenAt.HasValue || d.LastSeenAt < cutoff)
                .Select(d => new { d.DeviceId, d.DeviceName, d.Location, d.LastSeenAt })
                .ToList();

            var activeAlerts = await _alertService.GetActiveAlertsAsync();
            if (deviceId.HasValue)
                activeAlerts = activeAlerts.Where(a => a.DeviceId == deviceId.Value).ToList();

            object? focusDevice = null;
            if (deviceId.HasValue)
            {
                try
                {
                    var device = await _deviceService.GetDeviceByIdAsync(deviceId.Value);
                    focusDevice = new
                    {
                        device.DeviceId,
                        device.DeviceName,
                        device.Location,
                        device.IsActive,
                        device.LastSeenAt,
                        isOffline = !device.LastSeenAt.HasValue || device.LastSeenAt < cutoff
                    };
                }
                catch (KeyNotFoundException)
                {
                    focusDevice = new { error = $"Device {deviceId} not found." };
                }
            }

            return JsonSerializer.Serialize(new
            {
                dataAsOfUtc = DateTime.UtcNow,
                mqtt = new
                {
                    isConnected = _mqttRuntimeState.IsConnected,
                    isSubscribed = _mqttRuntimeState.IsSubscribed,
                    lastMessageReceivedAtUtc = _mqttRuntimeState.LastMessageReceivedAtUtc,
                    totalMessagesReceived = _mqttIngestMetrics.TotalMessagesReceived,
                    lastError = _mqttRuntimeState.LastError
                },
                devices = new { total = devices.Count, offlineCount = offline.Count, offline },
                alerts = new
                {
                    activeCount = activeAlerts.Count,
                    criticalCount = activeAlerts.Count(a => a.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase)),
                    highCount = activeAlerts.Count(a => a.Severity.Equals("high", StringComparison.OrdinalIgnoreCase))
                },
                focusDevice
            });
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

        private async Task<string> FindDevicesFromArgsAsync(JsonElement root) =>
            await FindDevicesJsonAsync(TryGetString(root, "query") ?? string.Empty);

        private async Task<string> FindActuatorsFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            return await FindActuatorsJsonAsync(deviceId, TryGetString(root, "query") ?? string.Empty);
        }

        private async Task<string> GetAlertSummaryFromArgsAsync(JsonElement root) =>
            await GetAlertSummaryJsonAsync(TryGetInt(root, "deviceId"));

        private async Task<string> GetSensorReadingSummaryFromArgsAsync(JsonElement root)
        {
            var deviceId = await ResolveDeviceIdAsync(root);
            var hours = Math.Clamp(TryGetInt(root, "hours") ?? 24, 1, 168);
            return await GetSensorReadingSummaryJsonAsync(deviceId, hours, TryGetInt(root, "sensorId"));
        }

        private async Task<string> GetOperationalSnapshotFromArgsAsync(JsonElement root) =>
            await GetOperationalSnapshotJsonAsync(TryGetInt(root, "deviceId"));

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

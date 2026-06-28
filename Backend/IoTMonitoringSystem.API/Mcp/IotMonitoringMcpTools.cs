using System.ComponentModel;
using IoTMonitoringSystem.API.Services;
using ModelContextProtocol.Server;

namespace IoTMonitoringSystem.API.Mcp
{
    /// <summary>
    /// MCP-exposed read-only tools for IoT monitoring data (devices, alerts, sensors, actuators, health, docs).
    /// Write actions remain in the dashboard assistant with human confirmation.
    /// </summary>
    [McpServerToolType]
    public sealed class IotMonitoringMcpTools
    {
        private readonly IIotAgentToolService _tools;

        public IotMonitoringMcpTools(IIotAgentToolService tools) => _tools = tools;

        [McpServerTool(Name = "get_devices")]
        [Description("Get all registered IoT devices with id, name, type, location, and activity status.")]
        public Task<string> GetDevices(CancellationToken cancellationToken) =>
            _tools.GetDevicesJsonAsync(cancellationToken);

        [McpServerTool(Name = "get_device")]
        [Description("Get one device by numeric deviceId including status and last seen time.")]
        public Task<string> GetDevice(
            [Description("Numeric device ID")] int deviceId,
            CancellationToken cancellationToken) =>
            _tools.GetDeviceJsonAsync(deviceId, cancellationToken);

        [McpServerTool(Name = "get_active_alerts")]
        [Description("Get all currently active alerts.")]
        public Task<string> GetActiveAlerts(CancellationToken cancellationToken) =>
            _tools.GetActiveAlertsJsonAsync(cancellationToken);

        [McpServerTool(Name = "get_alerts")]
        [Description("Get alerts with optional filters by deviceId, status, or severity.")]
        public Task<string> GetAlerts(
            [Description("Optional device ID filter")] int? deviceId,
            [Description("Active, Acknowledged, or Resolved")] string? status,
            [Description("low, medium, high, or critical")] string? severity,
            CancellationToken cancellationToken) =>
            _tools.GetAlertsJsonAsync(deviceId, status, severity, cancellationToken);

        [McpServerTool(Name = "get_sensors_by_device")]
        [Description("Get all sensors configured on a device.")]
        public Task<string> GetSensorsByDevice(
            [Description("Numeric device ID")] int deviceId,
            CancellationToken cancellationToken) =>
            _tools.GetSensorsByDeviceJsonAsync(deviceId, cancellationToken);

        [McpServerTool(Name = "get_actuators_by_device")]
        [Description("Get actuators on a device including LastKnownState (on/off), parsedIsOn, and toggleWouldSetOn.")]
        public Task<string> GetActuatorsByDevice(
            [Description("Numeric device ID")] int deviceId,
            CancellationToken cancellationToken) =>
            _tools.GetActuatorsByDeviceJsonAsync(deviceId, cancellationToken);

        [McpServerTool(Name = "get_recent_readings")]
        [Description("Get recent sensor readings for a device over the last N hours (default 24, max 168).")]
        public Task<string> GetRecentReadings(
            [Description("Numeric device ID")] int deviceId,
            [Description("Hours of history, 1-168 (default 24)")] int hours = 24,
            CancellationToken cancellationToken = default) =>
            _tools.GetRecentReadingsJsonAsync(deviceId, hours <= 0 ? 24 : hours, cancellationToken);

        [McpServerTool(Name = "get_system_health")]
        [Description("Get API and MQTT pipeline health including connection status and message counts.")]
        public Task<string> GetSystemHealth(CancellationToken cancellationToken) =>
            _tools.GetSystemHealthJsonAsync(cancellationToken);

        [McpServerTool(Name = "search_documentation")]
        [Description("Search project documentation (README, user manual, API docs, deployment guide) for setup and troubleshooting.")]
        public Task<string> SearchDocumentation(
            [Description("Search query for project docs")] string query,
            CancellationToken cancellationToken) =>
            _tools.SearchDocumentationJsonAsync(query, cancellationToken);

        [McpServerTool(Name = "get_alert_summary")]
        [Description("Summary of active alerts with counts by severity.")]
        public Task<string> GetAlertSummary(
            [Description("Optional device ID filter")] int? deviceId,
            CancellationToken cancellationToken) =>
            _tools.GetAlertSummaryJsonAsync(deviceId, cancellationToken);

        [McpServerTool(Name = "get_operational_snapshot")]
        [Description("Correlated MQTT health, offline devices, and alert counts.")]
        public Task<string> GetOperationalSnapshot(
            [Description("Optional focus device ID")] int? deviceId,
            CancellationToken cancellationToken) =>
            _tools.GetOperationalSnapshotJsonAsync(deviceId, cancellationToken);

        [McpServerTool(Name = "find_devices")]
        [Description("Search devices by name, location, or type.")]
        public Task<string> FindDevices(
            [Description("Search query; empty lists all")] string query,
            CancellationToken cancellationToken) =>
            _tools.FindDevicesJsonAsync(query, cancellationToken);
    }
}

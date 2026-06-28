namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Shared read-only IoT agent tools used by the in-process assistant and the MCP server.
    /// </summary>
    public interface IIotAgentToolService
    {
        Task<string> ExecuteAsync(string toolName, string argumentsJson);
        Task<string> GetDevicesJsonAsync(CancellationToken cancellationToken = default);
        Task<string> GetDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default);
        Task<string> GetActiveAlertsJsonAsync(CancellationToken cancellationToken = default);
        Task<string> GetAlertsJsonAsync(int? deviceId, string? status, string? severity, CancellationToken cancellationToken = default);
        Task<string> GetSensorsByDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default);
        Task<string> GetActuatorsByDeviceJsonAsync(int deviceId, CancellationToken cancellationToken = default);
        Task<string> GetRecentReadingsJsonAsync(int deviceId, int hours = 24, CancellationToken cancellationToken = default);
        Task<string> GetSystemHealthJsonAsync(CancellationToken cancellationToken = default);
        Task<string> SearchDocumentationJsonAsync(string query, CancellationToken cancellationToken = default);
    }
}

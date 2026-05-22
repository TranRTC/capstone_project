using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Services
{
    public interface IAlertService
    {
        Task<List<AlertDto>> GetActiveAlertsAsync();
        Task<PagedResult<AlertDto>> GetAlertsAsync(AlertQueryDto query);
        Task<AlertDto> GetAlertByIdAsync(long alertId);
        Task<AlertDto> AcknowledgeAlertAsync(long alertId);
        Task<AlertDto> ResolveAlertAsync(long alertId);
        Task DeleteAlertAsync(long alertId);
        Task<int> DeleteAlertsByDeviceAsync(int deviceId);
        Task<int> DeleteAlertsBulkAsync(AlertQueryDto query);
        Task EvaluateAlertRulesAsync(SensorReadingDto reading);
    }
}


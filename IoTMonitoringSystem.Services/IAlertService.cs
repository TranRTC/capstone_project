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
        Task EvaluateAlertRulesAsync(SensorReadingDto reading);
    }
}


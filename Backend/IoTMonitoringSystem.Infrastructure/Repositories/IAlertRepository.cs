using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IAlertRepository : IRepository<Alert>
    {
        Task<List<Alert>> GetActiveAlertsAsync();
        Task<List<Alert>> GetAlertsByDeviceIdAsync(int deviceId);
        Task<List<Alert>> GetAlertsByStatusAsync(string status);
        Task<List<Alert>> GetAlertsBySeverityAsync(string severity);
    }
}


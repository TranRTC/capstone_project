using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Core.Interfaces
{
    public interface IAgentAlertHandler
    {
        Task OnAlertCreatedAsync(AlertDto alert, CancellationToken cancellationToken = default);
        Task OnAlertUpdatedAsync(AlertDto alert, CancellationToken cancellationToken = default);
    }
}

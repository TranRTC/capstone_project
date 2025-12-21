using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.Core.Interfaces
{
    public interface INotificationService
    {
        Task NotifyNewAlertAsync(AlertDto alert);
        Task NotifyAlertAcknowledgedAsync(AlertDto alert);
        Task NotifyAlertResolvedAsync(AlertDto alert);
        Task NotifyAlertUpdatedAsync(AlertDto alert);
        Task NotifySensorReadingAsync(SensorReadingDto reading);
        Task NotifyDeviceStatusChangedAsync(DeviceStatusDto status);
    }
}


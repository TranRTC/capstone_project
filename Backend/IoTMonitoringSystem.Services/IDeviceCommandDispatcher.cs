using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Services
{
    public interface IDeviceCommandDispatcher
    {
        Task DispatchAsync(DeviceCommand command, CancellationToken cancellationToken = default);
    }
}

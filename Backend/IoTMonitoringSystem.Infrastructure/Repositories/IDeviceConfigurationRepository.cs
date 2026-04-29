using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IDeviceConfigurationRepository : IRepository<DeviceConfiguration>
    {
        Task<List<DeviceConfiguration>> GetByDeviceIdAsync(int deviceId);
        Task<DeviceConfiguration?> GetByDeviceIdAndKeyAsync(int deviceId, string configurationKey);
    }
}

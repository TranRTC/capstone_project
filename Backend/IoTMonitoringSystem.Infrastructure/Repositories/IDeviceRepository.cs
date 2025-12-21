using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IDeviceRepository : IRepository<Device>
    {
        Task<Device?> GetByEdgeDeviceIdAsync(string edgeDeviceId);
        Task<List<Device>> GetActiveDevicesAsync();
        Task<List<Device>> GetDevicesByTypeAsync(string deviceType);
    }
}


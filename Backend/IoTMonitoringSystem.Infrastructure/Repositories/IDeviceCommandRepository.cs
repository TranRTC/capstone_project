using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IDeviceCommandRepository : IRepository<DeviceCommand>
    {
        Task<List<DeviceCommand>> GetByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate, string? status);
        Task<DeviceCommand?> GetLatestByDeviceIdAsync(int deviceId);
    }
}

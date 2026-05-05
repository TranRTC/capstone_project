using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IActuatorRepository : IRepository<Actuator>
    {
        Task<List<Actuator>> GetByDeviceIdAsync(int deviceId);
        Task<Actuator?> GetByIdAndDeviceAsync(int actuatorId, int deviceId);
    }
}

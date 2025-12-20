using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface ISensorReadingRepository : IRepository<SensorReading>
    {
        Task<List<SensorReading>> GetByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate);
        Task<List<SensorReading>> GetBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate);
        Task<List<SensorReading>> GetByDeviceAndSensorAsync(int deviceId, int sensorId, DateTime? startDate, DateTime? endDate);
    }
}


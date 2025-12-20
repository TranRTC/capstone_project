using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class SensorReadingRepository : Repository<SensorReading>, ISensorReadingRepository
    {
        public SensorReadingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<SensorReading>> GetByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate)
        {
            var query = _dbSet.Where(sr => sr.DeviceId == deviceId);

            if (startDate.HasValue)
                query = query.Where(sr => sr.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sr => sr.Timestamp <= endDate.Value);

            return await query
                .OrderBy(sr => sr.Timestamp)
                .ToListAsync();
        }

        public async Task<List<SensorReading>> GetBySensorIdAsync(int sensorId, DateTime? startDate, DateTime? endDate)
        {
            var query = _dbSet.Where(sr => sr.SensorId == sensorId);

            if (startDate.HasValue)
                query = query.Where(sr => sr.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sr => sr.Timestamp <= endDate.Value);

            return await query
                .OrderBy(sr => sr.Timestamp)
                .ToListAsync();
        }

        public async Task<List<SensorReading>> GetByDeviceAndSensorAsync(int deviceId, int sensorId, DateTime? startDate, DateTime? endDate)
        {
            var query = _dbSet.Where(sr => sr.DeviceId == deviceId && sr.SensorId == sensorId);

            if (startDate.HasValue)
                query = query.Where(sr => sr.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sr => sr.Timestamp <= endDate.Value);

            return await query
                .OrderBy(sr => sr.Timestamp)
                .ToListAsync();
        }
    }
}


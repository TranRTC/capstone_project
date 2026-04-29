using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class DeviceConfigurationRepository : Repository<DeviceConfiguration>, IDeviceConfigurationRepository
    {
        public DeviceConfigurationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<DeviceConfiguration>> GetByDeviceIdAsync(int deviceId)
        {
            return await _dbSet
                .Where(c => c.DeviceId == deviceId)
                .OrderBy(c => c.ConfigurationKey)
                .ToListAsync();
        }

        public async Task<DeviceConfiguration?> GetByDeviceIdAndKeyAsync(int deviceId, string configurationKey)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.DeviceId == deviceId && c.ConfigurationKey == configurationKey);
        }
    }
}

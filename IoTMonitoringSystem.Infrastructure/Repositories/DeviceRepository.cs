using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class DeviceRepository : Repository<Device>, IDeviceRepository
    {
        public DeviceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Device?> GetByEdgeDeviceIdAsync(string edgeDeviceId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.EdgeDeviceId == edgeDeviceId);
        }

        public async Task<List<Device>> GetActiveDevicesAsync()
        {
            return await _dbSet
                .Where(d => d.IsActive)
                .ToListAsync();
        }

        public async Task<List<Device>> GetDevicesByTypeAsync(string deviceType)
        {
            return await _dbSet
                .Where(d => d.DeviceType == deviceType)
                .ToListAsync();
        }
    }
}


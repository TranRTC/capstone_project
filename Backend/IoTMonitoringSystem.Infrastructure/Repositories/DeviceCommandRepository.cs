using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class DeviceCommandRepository : Repository<DeviceCommand>, IDeviceCommandRepository
    {
        public DeviceCommandRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<DeviceCommand>> GetByDeviceIdAsync(int deviceId, DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _dbSet.Where(c => c.DeviceId == deviceId);

            if (startDate.HasValue)
                query = query.Where(c => c.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.CreatedAt <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<DeviceCommand?> GetLatestByDeviceIdAsync(int deviceId)
        {
            return await _dbSet
                .Where(c => c.DeviceId == deviceId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}

using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class ActuatorRepository : Repository<Actuator>, IActuatorRepository
    {
        public ActuatorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Actuator>> GetByDeviceIdAsync(int deviceId)
        {
            return await _dbSet
                .Where(a => a.DeviceId == deviceId)
                .OrderBy(a => a.Name)
                .ToListAsync();
        }

        public async Task<Actuator?> GetByIdAndDeviceAsync(int actuatorId, int deviceId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.ActuatorId == actuatorId && a.DeviceId == deviceId);
        }
    }
}

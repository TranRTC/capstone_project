using Microsoft.EntityFrameworkCore;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class AlertRepository : Repository<Alert>, IAlertRepository
    {
        public AlertRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Alert>> GetActiveAlertsAsync()
        {
            return await _dbSet
                .Where(a => a.Status == "Active")
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAlertsByDeviceIdAsync(int deviceId)
        {
            return await _dbSet
                .Where(a => a.DeviceId == deviceId)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAlertsByStatusAsync(string status)
        {
            return await _dbSet
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetAlertsBySeverityAsync(string severity)
        {
            return await _dbSet
                .Where(a => a.Severity == severity)
                .OrderByDescending(a => a.TriggeredAt)
                .ToListAsync();
        }
    }
}


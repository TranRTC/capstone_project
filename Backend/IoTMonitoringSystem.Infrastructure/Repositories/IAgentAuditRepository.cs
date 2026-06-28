using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IAgentAuditRepository
    {
        Task<AgentAuditLog> CreateAsync(AgentAuditLog log);
        Task<List<AgentAuditLog>> GetRecentAsync(int take = 100, string? eventType = null);
        Task<AgentMetricsDto> GetMetricsAsync(DateTime sinceUtc);
    }
}

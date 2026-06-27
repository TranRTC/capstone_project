using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IAgentInsightRepository
    {
        Task<AgentInsight> CreateAsync(AgentInsight insight);
        Task<AgentInsight?> GetByIdAsync(long id);
        Task<List<AgentInsight>> GetInsightsAsync(string? status, int pageNumber, int pageSize);
        Task<int> CountAsync(string? status);
        Task<int> CountSinceAsync(DateTime sinceUtc);
        Task<bool> ExistsRecentByDedupeKeyAsync(string dedupeKey, DateTime sinceUtc);
        Task<AgentInsight> DismissAsync(long id);
    }
}

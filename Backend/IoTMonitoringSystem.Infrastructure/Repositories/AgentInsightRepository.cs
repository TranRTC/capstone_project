using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class AgentInsightRepository : IAgentInsightRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentInsightRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AgentInsight> CreateAsync(AgentInsight insight)
        {
            _context.AgentInsights.Add(insight);
            await _context.SaveChangesAsync();
            return insight;
        }

        public async Task<AgentInsight?> GetByIdAsync(long id) =>
            await _context.AgentInsights.FindAsync(id);

        public async Task<List<AgentInsight>> GetInsightsAsync(string? status, int pageNumber, int pageSize)
        {
            var query = _context.AgentInsights.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? status)
        {
            var query = _context.AgentInsights.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);
            return await query.CountAsync();
        }

        public async Task<int> CountSinceAsync(DateTime sinceUtc) =>
            await _context.AgentInsights.CountAsync(i => i.CreatedAt >= sinceUtc);

        public async Task<bool> ExistsRecentByDedupeKeyAsync(string dedupeKey, DateTime sinceUtc) =>
            await _context.AgentInsights.AnyAsync(i =>
                i.DedupeKey == dedupeKey && i.CreatedAt >= sinceUtc);

        public async Task<AgentInsight> DismissAsync(long id)
        {
            var insight = await _context.AgentInsights.FindAsync(id)
                ?? throw new KeyNotFoundException($"Agent insight {id} not found.");

            insight.Status = "Dismissed";
            insight.DismissedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return insight;
        }
    }
}

using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class AgentAuditRepository : IAgentAuditRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentAuditRepository(ApplicationDbContext context) => _context = context;

        public async Task<AgentAuditLog> CreateAsync(AgentAuditLog log)
        {
            _context.AgentAuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<List<AgentAuditLog>> GetRecentAsync(int take = 100, string? eventType = null)
        {
            var query = _context.AgentAuditLogs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(l => l.EventType == eventType);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<AgentMetricsDto> GetMetricsAsync(DateTime sinceUtc)
        {
            var logs = await _context.AgentAuditLogs
                .Where(l => l.CreatedAt >= sinceUtc)
                .ToListAsync();

            var chatLogs = logs.Where(l => l.EventType == "Chat").ToList();
            var toolLogs = logs.Where(l => l.EventType == "ToolCall").ToList();

            return new AgentMetricsDto
            {
                ChatRequestsLast24h = chatLogs.Count,
                ToolCallsLast24h = toolLogs.Count,
                ActionsConfirmedLast24h = logs.Count(l => l.EventType == "ActionConfirmed"),
                LoopLimitHitsLast24h = logs.Count(l => l.EventType == "LoopLimit"),
                LlmErrorsLast24h = logs.Count(l => l.EventType == "LlmError"),
                AverageChatDurationMs = chatLogs.Count > 0
                    ? chatLogs.Where(l => l.DurationMs.HasValue).Select(l => l.DurationMs!.Value).DefaultIfEmpty(0).Average()
                    : 0,
                TopTools = toolLogs
                    .Where(l => !string.IsNullOrWhiteSpace(l.ToolName))
                    .GroupBy(l => l.ToolName!)
                    .Select(g => new AgentToolUsageDto { ToolName = g.Key, Count = g.Count() })
                    .OrderByDescending(t => t.Count)
                    .Take(10)
                    .ToList(),
                GeneratedAtUtc = DateTime.UtcNow
            };
        }
    }
}

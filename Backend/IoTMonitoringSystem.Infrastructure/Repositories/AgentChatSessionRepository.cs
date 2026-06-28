using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IAgentChatSessionRepository
    {
        Task<AgentChatSession?> GetByIdForUserAsync(long sessionId, string username);
        Task<AgentChatSession?> GetByIdAsync(long sessionId);
        Task<AgentChatSession> CreateAsync(AgentChatSession session);
        Task<AgentChatSession> UpdateAsync(AgentChatSession session);
        Task<List<AgentChatSession>> GetRecentForUserAsync(string username, int take = 10);
    }

    public class AgentChatSessionRepository : IAgentChatSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentChatSessionRepository(ApplicationDbContext context) => _context = context;

        public async Task<AgentChatSession?> GetByIdForUserAsync(long sessionId, string username) =>
            await _context.AgentChatSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.AgentChatSessionId == sessionId && s.Username == username);

        public async Task<AgentChatSession?> GetByIdAsync(long sessionId) =>
            await _context.AgentChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s => s.AgentChatSessionId == sessionId);

        public async Task<AgentChatSession> CreateAsync(AgentChatSession session)
        {
            _context.AgentChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AgentChatSession> UpdateAsync(AgentChatSession session)
        {
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<List<AgentChatSession>> GetRecentForUserAsync(string username, int take = 10) =>
            await _context.AgentChatSessions
                .Where(s => s.Username == username)
                .OrderByDescending(s => s.UpdatedAt)
                .Take(take)
                .ToListAsync();
    }
}

using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public class AgentActionProposalRepository : IAgentActionProposalRepository
    {
        private readonly ApplicationDbContext _context;

        public AgentActionProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AgentActionProposal> CreateAsync(AgentActionProposal proposal)
        {
            _context.AgentActionProposals.Add(proposal);
            await _context.SaveChangesAsync();
            return proposal;
        }

        public async Task<AgentActionProposal?> GetByIdAsync(long id) =>
            await _context.AgentActionProposals.FindAsync(id);

        public async Task<AgentActionProposal?> GetPendingForUserAsync(string username) =>
            await _context.AgentActionProposals
                .Where(p => p.Username == username && p.Status == "Pending" && p.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

        public async Task<AgentActionProposal> UpdateAsync(AgentActionProposal proposal)
        {
            await _context.SaveChangesAsync();
            return proposal;
        }

        public async Task ExpireStaleAsync(DateTime nowUtc)
        {
            var stale = await _context.AgentActionProposals
                .Where(p => p.Status == "Pending" && p.ExpiresAt <= nowUtc)
                .ToListAsync();

            foreach (var proposal in stale)
            {
                proposal.Status = "Expired";
            }

            if (stale.Count > 0)
                await _context.SaveChangesAsync();
        }
    }
}

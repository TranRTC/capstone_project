using IoTMonitoringSystem.Core.Entities;

namespace IoTMonitoringSystem.Infrastructure.Repositories
{
    public interface IAgentActionProposalRepository
    {
        Task<AgentActionProposal> CreateAsync(AgentActionProposal proposal);
        Task<AgentActionProposal?> GetByIdAsync(long id);
        Task<AgentActionProposal?> GetPendingForUserAsync(string username);
        Task<AgentActionProposal> UpdateAsync(AgentActionProposal proposal);
        Task ExpireStaleAsync(DateTime nowUtc);
    }
}

using System.Security.Claims;
using IoTMonitoringSystem.Core.DTOs;

namespace IoTMonitoringSystem.API.Services
{
    public interface IAgentService
    {
        AgentStatusDto GetStatus();
        Task<AgentChatResponse> ChatAsync(AgentChatRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    }
}

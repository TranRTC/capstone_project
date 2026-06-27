using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Interfaces;

namespace IoTMonitoringSystem.API.Services
{
    public class AgentAlertEventHandler : IAgentAlertHandler
    {
        private readonly IProactiveAgentService _proactiveAgentService;
        private readonly ILogger<AgentAlertEventHandler> _logger;

        public AgentAlertEventHandler(
            IProactiveAgentService proactiveAgentService,
            ILogger<AgentAlertEventHandler> logger)
        {
            _proactiveAgentService = proactiveAgentService;
            _logger = logger;
        }

        public async Task OnAlertCreatedAsync(AlertDto alert, CancellationToken cancellationToken = default)
        {
            try
            {
                await _proactiveAgentService.HandleNewAlertAsync(alert, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create proactive insight for alert {AlertId}", alert.AlertId);
            }
        }

        public Task OnAlertUpdatedAsync(AlertDto alert, CancellationToken cancellationToken = default)
        {
            // v2: only generate insights for newly created alerts, not every update.
            return Task.CompletedTask;
        }
    }
}

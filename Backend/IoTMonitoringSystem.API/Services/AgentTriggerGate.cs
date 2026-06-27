namespace IoTMonitoringSystem.API.Services
{
    public class AgentTriggerGate
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentTriggerGate> _logger;

        public AgentTriggerGate(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<AgentTriggerGate> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ShouldProcessAsync(string dedupeKey, CancellationToken cancellationToken = default)
        {
            var cooldownMinutes = _configuration.GetValue("Agent:Proactive:InsightCooldownMinutes", 15);
            var maxPerHour = _configuration.GetValue("Agent:Proactive:MaxInsightsPerHour", 20);
            var sinceCooldown = DateTime.UtcNow.AddMinutes(-cooldownMinutes);
            var sinceHour = DateTime.UtcNow.AddHours(-1);

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<Infrastructure.Repositories.IAgentInsightRepository>();

            if (await repository.ExistsRecentByDedupeKeyAsync(dedupeKey, sinceCooldown))
            {
                _logger.LogDebug("Proactive insight suppressed by cooldown for {DedupeKey}", dedupeKey);
                return false;
            }

            if (await repository.CountSinceAsync(sinceHour) >= maxPerHour)
            {
                _logger.LogWarning("Proactive insight suppressed by hourly cap for {DedupeKey}", dedupeKey);
                return false;
            }

            return true;
        }
    }
}

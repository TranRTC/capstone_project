namespace IoTMonitoringSystem.API.Services
{
    public class AgentActionExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentActionExpiryBackgroundService> _logger;

        public AgentActionExpiryBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AgentActionExpiryBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Agent:Actions:Enabled", true))
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Interval, stoppingToken);
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<Infrastructure.Repositories.IAgentActionProposalRepository>();
                    await repository.ExpireStaleAsync(DateTime.UtcNow);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent action expiry sweep failed.");
                }
            }
        }
    }
}

namespace IoTMonitoringSystem.API.Services
{
    public class AgentMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentMonitoringBackgroundService> _logger;

        public AgentMonitoringBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AgentMonitoringBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Agent:Proactive:Enabled", true))
            {
                _logger.LogInformation("AgentMonitoringBackgroundService disabled via config.");
                return;
            }

            var intervalSeconds = _configuration.GetValue("Agent:Proactive:SweepIntervalSeconds", 60);
            _logger.LogInformation(
                "AgentMonitoringBackgroundService started. Sweep interval {IntervalSeconds}s.",
                intervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                    using var scope = _serviceProvider.CreateScope();
                    var proactive = scope.ServiceProvider.GetRequiredService<IProactiveAgentService>();
                    await proactive.RunMonitoringSweepAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent monitoring sweep failed.");
                }
            }
        }
    }
}

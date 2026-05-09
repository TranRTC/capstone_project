using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Background service that periodically marks stale Pending/Sent commands as Timeout.
    /// Runs every minute and times out commands older than the configured threshold.
    /// </summary>
    public class CommandTimeoutService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandTimeoutService> _logger;
        private readonly TimeSpan _timeoutThreshold;

        public CommandTimeoutService(
            IServiceProvider serviceProvider,
            ILogger<CommandTimeoutService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // Default: commands pending/sent for more than 5 minutes → Timeout
            var minutes = configuration.GetValue<int>("Commands:TimeoutMinutes", 5);
            _timeoutThreshold = TimeSpan.FromMinutes(minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "CommandTimeoutService started. Checking every {CheckInterval}s, timeout threshold {Threshold}min.",
                CheckInterval.TotalSeconds,
                _timeoutThreshold.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                    await TimeoutStaleCommandsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CommandTimeoutService sweep.");
                }
            }
        }

        private async Task TimeoutStaleCommandsAsync(CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow - _timeoutThreshold;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var stale = await db.DeviceCommands
                .Where(c => (c.Status == "Pending" || c.Status == "Sent") && c.CreatedAt < cutoff)
                .ToListAsync(cancellationToken);

            if (stale.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var command in stale)
            {
                command.Status = "Timeout";
                command.CompletedAt = now;
                command.ErrorMessage = $"No acknowledgement received within {_timeoutThreshold.TotalMinutes} minutes.";
            }

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Timed out {Count} stale command(s) older than {Threshold} minutes.",
                stale.Count,
                _timeoutThreshold.TotalMinutes);
        }
    }
}

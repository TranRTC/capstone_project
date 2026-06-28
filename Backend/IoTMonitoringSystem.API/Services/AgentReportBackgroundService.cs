namespace IoTMonitoringSystem.API.Services
{
    public class AgentReportBackgroundService : BackgroundService
    {
        private static DateTime? _lastDailyDigestLocalDate;
        private static DateTime? _lastWeeklyDigestWeekStart;

        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentReportBackgroundService> _logger;

        public AgentReportBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AgentReportBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Agent:ScheduledReports:Enabled", true))
            {
                _logger.LogInformation("AgentReportBackgroundService disabled via config.");
                return;
            }

            _logger.LogInformation("AgentReportBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    await TryRunScheduledDigestsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduled digest check failed.");
                }
            }
        }

        private async Task TryRunScheduledDigestsAsync(CancellationToken stoppingToken)
        {
            var localNow = GetLocalNow();
            var dailyHour = _configuration.GetValue("Agent:ScheduledReports:DailyDigestHourLocal", 8);

            if (localNow.Hour == dailyHour && localNow.Minute == 0)
            {
                if (_lastDailyDigestLocalDate != localNow.Date)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var proactive = scope.ServiceProvider.GetRequiredService<IProactiveAgentService>();
                    var insight = await proactive.RunDailyDigestAsync(force: false, stoppingToken);
                    _lastDailyDigestLocalDate = localNow.Date;
                    if (insight is not null)
                        _logger.LogInformation("Daily digest insight {Id} created.", insight.AgentInsightId);
                }
            }

            var weeklyDay = _configuration["Agent:ScheduledReports:WeeklyDigestDay"] ?? "Monday";
            if (localNow.DayOfWeek.ToString().Equals(weeklyDay, StringComparison.OrdinalIgnoreCase) &&
                localNow.Hour == dailyHour &&
                localNow.Minute == 0)
            {
                var weekStart = GetWeekStart(localNow, weeklyDay);
                if (_lastWeeklyDigestWeekStart != weekStart)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var proactive = scope.ServiceProvider.GetRequiredService<IProactiveAgentService>();
                    var insight = await proactive.RunWeeklyDigestAsync(force: false, stoppingToken);
                    _lastWeeklyDigestWeekStart = weekStart;
                    if (insight is not null)
                        _logger.LogInformation("Weekly digest insight {Id} created.", insight.AgentInsightId);
                }
            }
        }

        private DateTime GetLocalNow()
        {
            var timeZoneId = _configuration["Agent:ScheduledReports:Timezone"] ?? "Central Standard Time";
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        private static DateTime GetWeekStart(DateTime localDate, string configuredDay)
        {
            if (!Enum.TryParse<DayOfWeek>(configuredDay, true, out var targetDay))
                targetDay = DayOfWeek.Monday;

            var diff = ((7 + (localDate.DayOfWeek - targetDay)) % 7);
            return localDate.Date.AddDays(-diff);
        }
    }
}

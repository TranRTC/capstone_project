using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IoTMonitoringSystem.Services
{
    public class AlertService : IAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationService? _notificationService;
        private readonly IServiceScopeFactory? _scopeFactory;

        public AlertService(
            ApplicationDbContext context,
            IAlertRepository alertRepository,
            INotificationService? notificationService = null,
            IServiceScopeFactory? scopeFactory = null)
        {
            _context = context;
            _alertRepository = alertRepository;
            _notificationService = notificationService;
            _scopeFactory = scopeFactory;
        }

        public async Task<List<AlertDto>> GetActiveAlertsAsync()
        {
            var alerts = await _alertRepository.GetActiveAlertsAsync();
            return alerts.Select(MapToAlertDto).ToList();
        }

        public async Task<PagedResult<AlertDto>> GetAlertsAsync(AlertQueryDto query)
        {
            var dbQuery = _context.Alerts
                .Include(a => a.AlertRule)
                .Include(a => a.Device)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.Status))
                dbQuery = dbQuery.Where(a => a.Status == query.Status);

            if (!string.IsNullOrEmpty(query.Severity))
                dbQuery = dbQuery.Where(a => a.Severity == query.Severity);

            if (query.DeviceId.HasValue)
                dbQuery = dbQuery.Where(a => a.DeviceId == query.DeviceId.Value);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(a => a.TriggeredAt >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(a => a.TriggeredAt <= query.EndDate.Value);

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderByDescending(a => a.TriggeredAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<AlertDto>
            {
                Items = items.Select(MapToAlertDto).ToList(),
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<AlertDto> GetAlertByIdAsync(long alertId)
        {
            var alert = await _context.Alerts
                .Include(a => a.AlertRule)
                .Include(a => a.Device)
                .FirstOrDefaultAsync(a => a.AlertId == alertId);

            if (alert == null)
                throw new KeyNotFoundException($"Alert with ID {alertId} not found");

            return MapToAlertDto(alert);
        }

        public async Task<AlertDto> AcknowledgeAlertAsync(long alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null)
                throw new KeyNotFoundException($"Alert with ID {alertId} not found");

            alert.AcknowledgedAt = DateTime.UtcNow;
            var updatedAlert = await _alertRepository.UpdateAsync(alert);
            var alertDto = MapToAlertDto(updatedAlert);

            // Notify via SignalR
            if (_notificationService != null)
                await _notificationService.NotifyAlertAcknowledgedAsync(alertDto);

            return alertDto;
        }

        public async Task<AlertDto> ResolveAlertAsync(long alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null)
                throw new KeyNotFoundException($"Alert with ID {alertId} not found");

            alert.Status = "Resolved";
            alert.ResolvedAt = DateTime.UtcNow;
            var updatedAlert = await _alertRepository.UpdateAsync(alert);
            var alertDto = MapToAlertDto(updatedAlert);

            // Notify via SignalR
            if (_notificationService != null)
                await _notificationService.NotifyAlertResolvedAsync(alertDto);

            return alertDto;
        }

        public async Task DeleteAlertAsync(long alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null)
                throw new KeyNotFoundException($"Alert with ID {alertId} not found");

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
        }

        public Task<int> DeleteAlertsByDeviceAsync(int deviceId)
        {
            return DeleteAlertsBulkAsync(new AlertQueryDto { DeviceId = deviceId });
        }

        public async Task<int> DeleteAlertsBulkAsync(AlertQueryDto query)
        {
            if (!query.DeviceId.HasValue)
                throw new ArgumentException("deviceId is required for bulk delete.", nameof(query));

            var dbQuery = BuildFilteredAlertsQuery(query);
            var alerts = await dbQuery.ToListAsync();
            if (alerts.Count == 0)
                return 0;

            _context.Alerts.RemoveRange(alerts);
            await _context.SaveChangesAsync();
            return alerts.Count;
        }

        private IQueryable<Alert> BuildFilteredAlertsQuery(AlertQueryDto query)
        {
            var dbQuery = _context.Alerts.AsQueryable();

            if (!string.IsNullOrEmpty(query.Status))
                dbQuery = dbQuery.Where(a => a.Status == query.Status);

            if (!string.IsNullOrEmpty(query.Severity))
                dbQuery = dbQuery.Where(a => a.Severity == query.Severity);

            if (query.DeviceId.HasValue)
                dbQuery = dbQuery.Where(a => a.DeviceId == query.DeviceId.Value);

            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(a => a.TriggeredAt >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(a => a.TriggeredAt <= query.EndDate.Value);

            return dbQuery;
        }

        public async Task EvaluateAlertRulesAsync(SensorReadingDto reading)
        {
            // Get all enabled alert rules that match this device and/or sensor
            var alertRules = await _context.AlertRules
                .Where(ar => ar.IsEnabled &&
                    ((ar.DeviceId == null || ar.DeviceId == reading.DeviceId) &&
                     (ar.SensorId == null || ar.SensorId == reading.SensorId)))
                .ToListAsync();

            foreach (var rule in alertRules)
            {
                bool shouldTrigger = false;

                // Evaluate based on rule type
                switch (rule.RuleType.ToLower())
                {
                    case "threshold":
                        shouldTrigger = EvaluateThresholdRule(rule, reading.Value);
                        break;
                    case "range":
                        shouldTrigger = EvaluateRangeRule(rule, reading.Value);
                        break;
                    case "change":
                        shouldTrigger = await EvaluateChangeRuleAsync(rule, reading);
                        break;

                    default:
                        continue;
                }

                if (shouldTrigger)
                {
                    await CreateAlertAsync(rule, reading);
                }
            }
        }

        private bool EvaluateThresholdRule(AlertRule rule, decimal value)
        {
            if (!rule.ThresholdValue.HasValue)
                return false;

            var threshold = rule.ThresholdValue.Value;
            var comparisonOperator = rule.ComparisonOperator?.ToLower() ?? ">";

            return comparisonOperator switch
            {
                ">" => value > threshold,
                ">=" => value >= threshold,
                "<" => value < threshold,
                "<=" => value <= threshold,
                "==" => value == threshold,
                "!=" => value != threshold,
                _ => false
            };
        }

        private bool EvaluateRangeRule(AlertRule rule, decimal value)
        {
            if (!rule.MinValue.HasValue || !rule.MaxValue.HasValue)
                return false;

            return value < rule.MinValue.Value || value > rule.MaxValue.Value;
        }

        private async Task<bool> EvaluateChangeRuleAsync(AlertRule rule, SensorReadingDto reading)
        {
            var previousReading = await _context.SensorReadings
                .Where(r => r.SensorId == reading.SensorId &&
                            r.DeviceId == reading.DeviceId &&
                            r.Timestamp < reading.Timestamp)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (previousReading == null)
                return false;

            return previousReading.Value != reading.Value;
        }

        private async Task CreateAlertAsync(AlertRule rule, SensorReadingDto reading)
        {
            // Check if there's already an active alert for this rule
            var existingAlert = await _context.Alerts
                .FirstOrDefaultAsync(a => a.AlertRuleId == rule.AlertRuleId &&
                                         a.Status == "Active" &&
                                         a.DeviceId == reading.DeviceId);

            if (existingAlert != null)
            {
                // Update existing alert instead of creating duplicate
                existingAlert.TriggerValue = reading.Value;
                existingAlert.TriggeredAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(existingAlert);
                var updatedAlertDto = MapToAlertDto(existingAlert);

                // Notify via SignalR
                if (_notificationService != null)
                    await _notificationService.NotifyAlertUpdatedAsync(updatedAlertDto);

                return;
            }

            var alert = new Alert
            {
                AlertRuleId = rule.AlertRuleId,
                DeviceId = reading.DeviceId,
                SensorId = reading.SensorId,
                Severity = rule.Severity,
                Message = rule.Condition,
                TriggerValue = reading.Value,
                Status = "Active",
                TriggeredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _alertRepository.CreateAsync(alert);
            var alertDto = MapToAlertDto(alert);

            // Notify via SignalR
            if (_notificationService != null)
                await _notificationService.NotifyNewAlertAsync(alertDto);

            await NotifyProactiveAgentAsync(alertDto);
        }

        private async Task NotifyProactiveAgentAsync(AlertDto alertDto)
        {
            if (_scopeFactory is null)
                return;

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetService<IAgentAlertHandler>();
            if (handler is not null)
                await handler.OnAlertCreatedAsync(alertDto);
        }

        private AlertDto MapToAlertDto(Alert alert)
        {
            return new AlertDto
            {
                AlertId = alert.AlertId,
                AlertRuleId = alert.AlertRuleId,
                DeviceId = alert.DeviceId,
                SensorId = alert.SensorId,
                Severity = alert.Severity,
                Message = alert.Message,
                TriggerValue = alert.TriggerValue,
                Status = alert.Status,
                TriggeredAt = alert.TriggeredAt,
                AcknowledgedAt = alert.AcknowledgedAt,
                ResolvedAt = alert.ResolvedAt,
                CreatedAt = alert.CreatedAt
            };
        }
    }
}


using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Core.Interfaces;
using IoTMonitoringSystem.Infrastructure.Data;
using IoTMonitoringSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Services
{
    public class AlertService : IAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationService? _notificationService;

        public AlertService(
            ApplicationDbContext context,
            IAlertRepository alertRepository,
            INotificationService? notificationService = null)
        {
            _context = context;
            _alertRepository = alertRepository;
            _notificationService = notificationService;
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
                        // For change detection, we'd need previous reading - simplified for now
                        shouldTrigger = false;
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
            // Range rules would need min/max values - simplified for now
            // This could be extended based on your requirements
            return false;
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


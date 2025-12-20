using IoTMonitoringSystem.Core.DTOs;
using IoTMonitoringSystem.Core.Entities;
using IoTMonitoringSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitoringSystem.Services
{
    public class AlertRuleService : IAlertRuleService
    {
        private readonly ApplicationDbContext _context;

        public AlertRuleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlertRuleDto>> GetAllAlertRulesAsync()
        {
            var rules = await _context.AlertRules
                .Include(ar => ar.Device)
                .Include(ar => ar.Sensor)
                .ToListAsync();

            return rules.Select(MapToAlertRuleDto).ToList();
        }

        public async Task<AlertRuleDto> GetAlertRuleByIdAsync(int alertRuleId)
        {
            var rule = await _context.AlertRules
                .Include(ar => ar.Device)
                .Include(ar => ar.Sensor)
                .FirstOrDefaultAsync(ar => ar.AlertRuleId == alertRuleId);

            if (rule == null)
                throw new KeyNotFoundException($"Alert rule with ID {alertRuleId} not found");

            return MapToAlertRuleDto(rule);
        }

        public async Task<AlertRuleDto> CreateAlertRuleAsync(CreateAlertRuleDto dto)
        {
            // Validate device if provided
            if (dto.DeviceId.HasValue)
            {
                var device = await _context.Devices.FindAsync(dto.DeviceId.Value);
                if (device == null)
                    throw new KeyNotFoundException($"Device with ID {dto.DeviceId} not found");
            }

            // Validate sensor if provided
            if (dto.SensorId.HasValue)
            {
                var sensor = await _context.Sensors.FindAsync(dto.SensorId.Value);
                if (sensor == null)
                    throw new KeyNotFoundException($"Sensor with ID {dto.SensorId} not found");
            }

            var rule = new AlertRule
            {
                DeviceId = dto.DeviceId,
                SensorId = dto.SensorId,
                RuleName = dto.RuleName,
                RuleType = dto.RuleType,
                Condition = dto.Condition,
                ThresholdValue = dto.ThresholdValue,
                ComparisonOperator = dto.ComparisonOperator,
                Severity = dto.Severity,
                IsEnabled = dto.IsEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.AlertRules.AddAsync(rule);
            await _context.SaveChangesAsync();

            return MapToAlertRuleDto(rule);
        }

        public async Task<AlertRuleDto> UpdateAlertRuleAsync(int alertRuleId, UpdateAlertRuleDto dto)
        {
            var rule = await _context.AlertRules.FindAsync(alertRuleId);
            if (rule == null)
                throw new KeyNotFoundException($"Alert rule with ID {alertRuleId} not found");

            if (dto.RuleName != null)
                rule.RuleName = dto.RuleName;
            if (dto.RuleType != null)
                rule.RuleType = dto.RuleType;
            if (dto.Condition != null)
                rule.Condition = dto.Condition;
            if (dto.ThresholdValue.HasValue)
                rule.ThresholdValue = dto.ThresholdValue;
            if (dto.ComparisonOperator != null)
                rule.ComparisonOperator = dto.ComparisonOperator;
            if (dto.Severity != null)
                rule.Severity = dto.Severity;
            if (dto.IsEnabled.HasValue)
                rule.IsEnabled = dto.IsEnabled.Value;

            rule.UpdatedAt = DateTime.UtcNow;

            _context.AlertRules.Update(rule);
            await _context.SaveChangesAsync();

            return MapToAlertRuleDto(rule);
        }

        public async Task DeleteAlertRuleAsync(int alertRuleId)
        {
            var rule = await _context.AlertRules.FindAsync(alertRuleId);
            if (rule == null)
                throw new KeyNotFoundException($"Alert rule with ID {alertRuleId} not found");

            _context.AlertRules.Remove(rule);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AlertRuleDto>> GetAlertRulesByDeviceIdAsync(int deviceId)
        {
            var rules = await _context.AlertRules
                .Where(ar => ar.DeviceId == deviceId)
                .Include(ar => ar.Device)
                .Include(ar => ar.Sensor)
                .ToListAsync();

            return rules.Select(MapToAlertRuleDto).ToList();
        }

        private AlertRuleDto MapToAlertRuleDto(AlertRule rule)
        {
            return new AlertRuleDto
            {
                AlertRuleId = rule.AlertRuleId,
                DeviceId = rule.DeviceId,
                SensorId = rule.SensorId,
                RuleName = rule.RuleName,
                RuleType = rule.RuleType,
                Condition = rule.Condition,
                ThresholdValue = rule.ThresholdValue,
                ComparisonOperator = rule.ComparisonOperator,
                Severity = rule.Severity,
                IsEnabled = rule.IsEnabled,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };
        }
    }
}


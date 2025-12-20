using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class AlertDto
    {
        public long AlertId { get; set; }
        public int AlertRuleId { get; set; }
        public int DeviceId { get; set; }
        public int? SensorId { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal? TriggerValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AlertQueryDto
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public int? DeviceId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}


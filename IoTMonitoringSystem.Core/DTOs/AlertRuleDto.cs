using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class CreateAlertRuleDto
    {
        public int? DeviceId { get; set; }

        public int? SensorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RuleType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Condition { get; set; } = string.Empty;

        public decimal? ThresholdValue { get; set; }

        [MaxLength(10)]
        public string? ComparisonOperator { get; set; }

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateAlertRuleDto
    {
        [MaxLength(100)]
        public string? RuleName { get; set; }

        [MaxLength(50)]
        public string? RuleType { get; set; }

        [MaxLength(200)]
        public string? Condition { get; set; }

        public decimal? ThresholdValue { get; set; }

        [MaxLength(10)]
        public string? ComparisonOperator { get; set; }

        [MaxLength(20)]
        public string? Severity { get; set; }

        public bool? IsEnabled { get; set; }
    }

    public class AlertRuleDto
    {
        public int AlertRuleId { get; set; }
        public int? DeviceId { get; set; }
        public int? SensorId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public decimal? ThresholdValue { get; set; }
        public string? ComparisonOperator { get; set; }
        public string Severity { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}


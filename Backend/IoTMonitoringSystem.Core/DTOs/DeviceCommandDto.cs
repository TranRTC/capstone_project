using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class CreateDeviceCommandDto
    {
        [Required]
        [MaxLength(50)]
        public string CommandType { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Payload { get; set; } = "{}";

        [MaxLength(100)]
        public string? CorrelationId { get; set; }
    }

    public class DeviceCommandDto
    {
        public long CommandId { get; set; }
        public int DeviceId { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public string Payload { get; set; } = "{}";
        public string Status { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequestedBy { get; set; }
    }

    public class DeviceCommandQueryDto
    {
        [MaxLength(20)]
        public string? Status { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}

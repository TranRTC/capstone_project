using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class CreateSensorReadingDto
    {
        [Required]
        public int DeviceId { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        public decimal Value { get; set; }

        public DateTime? Timestamp { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(20)]
        public string? Quality { get; set; }
    }

    public class SensorReadingDto
    {
        public long ReadingId { get; set; }
        public int DeviceId { get; set; }
        public int SensorId { get; set; }
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public string? Quality { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SensorReadingQueryDto
    {
        public int? DeviceId { get; set; }
        public int? SensorId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }

    public class BatchCreateSensorReadingDto
    {
        [Required]
        public List<CreateSensorReadingDto> Readings { get; set; } = new List<CreateSensorReadingDto>();
    }
}


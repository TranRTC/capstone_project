using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class CreateSensorDto
    {
        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SensorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SensorType { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Unit { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }
    }

    public class UpdateSensorDto
    {
        [MaxLength(100)]
        public string? SensorName { get; set; }

        [MaxLength(50)]
        public string? SensorType { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public bool? IsActive { get; set; }
    }

    public class SensorDto
    {
        public int SensorId { get; set; }
        public int DeviceId { get; set; }
        public string? EdgeDeviceId { get; set; }
        public string SensorName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}


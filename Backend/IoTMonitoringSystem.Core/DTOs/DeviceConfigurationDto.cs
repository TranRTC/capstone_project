using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class DeviceConfigurationDto
    {
        public int ConfigurationId { get; set; }
        public int DeviceId { get; set; }
        public string ConfigurationKey { get; set; } = string.Empty;
        public string? ConfigurationValue { get; set; }
        public string? ValueType { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DeviceConfigurationItemDto
    {
        [Required]
        [MaxLength(100)]
        public string ConfigurationKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ConfigurationValue { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpsertDeviceConfigurationsDto
    {
        [Required]
        public List<DeviceConfigurationItemDto> Configurations { get; set; } = new();
    }
}

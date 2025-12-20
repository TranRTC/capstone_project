using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    public class DeviceConfiguration
    {
        [Key]
        public int ConfigurationId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ConfigurationKey { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ConfigurationValue { get; set; }

        [MaxLength(20)]
        public string? ValueType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;
    }
}


using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    // Request DTOs
    public class CreateDeviceDto
    {
        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(50)]
        public string? EdgeDeviceType { get; set; }

        [MaxLength(100)]
        public string? EdgeDeviceId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateDeviceDto
    {
        [MaxLength(100)]
        public string? DeviceName { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? FacilityType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    // Response DTOs
    public class DeviceDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? FacilityType { get; set; }
        public string? EdgeDeviceType { get; set; }
        public string? EdgeDeviceId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public string? Description { get; set; }
    }

    public class DeviceListDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    public class DeviceStatusDto
    {
        public int DeviceId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}


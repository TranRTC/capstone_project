using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.DTOs
{
    public class ActuatorDto
    {
        public int ActuatorId { get; set; }
        public int DeviceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Discrete or Analog</summary>
        public string Kind { get; set; } = "Discrete";
        public string? Channel { get; set; }
        public decimal? AnalogMin { get; set; }
        public decimal? AnalogMax { get; set; }
        public string? ControlUnit { get; set; }
        public bool IsActive { get; set; }
        public int? FeedbackSensorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateActuatorDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Kind { get; set; } = "Discrete";

        [MaxLength(100)]
        public string? Channel { get; set; }

        public decimal? AnalogMin { get; set; }
        public decimal? AnalogMax { get; set; }

        [MaxLength(20)]
        public string? ControlUnit { get; set; }

        public int? FeedbackSensorId { get; set; }
    }

    public class UpdateActuatorDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Kind { get; set; }

        [MaxLength(100)]
        public string? Channel { get; set; }

        public decimal? AnalogMin { get; set; }
        public decimal? AnalogMax { get; set; }

        [MaxLength(20)]
        public string? ControlUnit { get; set; }

        public bool? IsActive { get; set; }

        public int? FeedbackSensorId { get; set; }
    }
}

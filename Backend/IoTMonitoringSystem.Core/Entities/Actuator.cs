using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitoringSystem.Core.Entities
{
    /// <summary>
    /// A controllable output point attached to a device (symmetric to Sensor for inputs).
    /// </summary>
    public class Actuator
    {
        [Key]
        public int ActuatorId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Discrete (SetPower) or Analog (SetValue).</summary>
        [Required]
        [MaxLength(20)]
        public string Kind { get; set; } = "Discrete";

        /// <summary>Optional routing hint for firmware (GPIO index, relay number, etc.).</summary>
        [MaxLength(100)]
        public string? Channel { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? AnalogMin { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? AnalogMax { get; set; }

        [MaxLength(20)]
        public string? ControlUnit { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Optional sensor whose readings reflect this actuator's physical state.</summary>
        public int? FeedbackSensorId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DeviceId")]
        public Device Device { get; set; } = null!;

        [ForeignKey("FeedbackSensorId")]
        public Sensor? FeedbackSensor { get; set; }

        public ICollection<DeviceCommand> DeviceCommands { get; set; } = new List<DeviceCommand>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace IoTMonitoringSystem.Core.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Admin or Viewer</summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Viewer";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

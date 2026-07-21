using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyHub.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    [StringLength(200)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? EntityName { get; set; }

    public string? EntityId { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? Browser { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool Success { get; set; } = true;

    [StringLength(2000)]
    [NotMapped]
    public string? Details { get; set; }
}

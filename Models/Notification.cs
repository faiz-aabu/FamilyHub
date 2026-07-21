using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyHub.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = "Information";

    [StringLength(200)]
    [Column("LinkUrl")]
    public string? Link { get; set; }

    [StringLength(100)]
    public string? Icon { get; set; }

    [StringLength(100)]
    public string? RelatedEntityType { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string GetTimeAgo()
    {
        var elapsed = DateTime.UtcNow - CreatedAt;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
            return $"{minutes}m ago";
        }

        if (elapsed < TimeSpan.FromDays(1))
        {
            var hours = Math.Max(1, (int)elapsed.TotalHours);
            return $"{hours}h ago";
        }

        if (elapsed < TimeSpan.FromDays(30))
        {
            var days = Math.Max(1, (int)elapsed.TotalDays);
            return $"{days}d ago";
        }

        return CreatedAt.ToLocalTime().ToString("dd MMM yyyy");
    }
}

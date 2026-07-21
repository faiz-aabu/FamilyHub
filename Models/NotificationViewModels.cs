namespace FamilyHub.Models;

public class NotificationListViewModel
{
    public string SearchTerm { get; set; } = string.Empty;

    public string? SelectedType { get; set; }

    public string SelectedReadFilter { get; set; } = "all";

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public IReadOnlyList<Notification> Notifications { get; set; } = Array.Empty<Notification>();

    public IReadOnlyList<string> AvailableTypes { get; set; } = Array.Empty<string>();
}

public class DashboardNotificationViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = "Information";

    public string? Link { get; set; }

    public string? Icon { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

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

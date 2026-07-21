namespace FamilyHub.Models;

public class ActivityLogIndexViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string ActionFilter { get; set; } = string.Empty;
    public string UserFilter { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public IReadOnlyList<ActivityLog> Logs { get; set; } = Array.Empty<ActivityLog>();
    public Dictionary<string, int> Statistics { get; set; } = new();
    public IReadOnlyList<string> AvailableActions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AvailableUsers { get; set; } = Array.Empty<string>();
}

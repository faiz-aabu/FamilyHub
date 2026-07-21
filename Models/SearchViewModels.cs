using System.Text.Encodings.Web;

namespace FamilyHub.Models;

public class SearchViewModel
{
    public string Query { get; set; } = string.Empty;

    public string SortBy { get; set; } = "relevance";

    public string? Gender { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public string? Nationality { get; set; }

    public string? State { get; set; }

    public string? Religion { get; set; }

    public string? Occupation { get; set; }

    public string? MaritalStatus { get; set; }

    public string? RelationshipType { get; set; }

    public string? FamilyBranch { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public DateTime? UpdatedFrom { get; set; }

    public DateTime? UpdatedTo { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 6;

    public bool ShowFilters { get; set; }

    public bool HasQuery { get; set; }

    public bool HasResults { get; set; }

    public string? ErrorMessage { get; set; }

    public IReadOnlyList<SearchResultItemViewModel> FamilyMembers { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<SearchResultItemViewModel> Relationships { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<SearchResultItemViewModel> Users { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<SearchResultItemViewModel> Notifications { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<SearchResultItemViewModel> AuditLogs { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<SearchResultItemViewModel> Reports { get; set; } = Array.Empty<SearchResultItemViewModel>();

    public IReadOnlyList<string> RecentSearches { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableGenders { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableNationalities { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableStates { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableOccupations { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableMaritalStatuses { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> AvailableRelationshipTypes { get; set; } = Array.Empty<string>();

    public string SearchSummary => string.IsNullOrWhiteSpace(Query)
        ? "Start with a name, email, phone number, service, or relationship detail."
        : $"Showing the latest matches for '{Query}'.";
}

public class SearchResultItemViewModel
{
    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Icon { get; set; } = "bi-search";

    public string Badge { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? Age { get; set; }

    public string HighlightedTitle { get; set; } = string.Empty;

    public string HighlightedSubtitle { get; set; } = string.Empty;

    public string HighlightedDescription { get; set; } = string.Empty;

    public static string Highlight(string? value, string? query)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return HtmlEncoder.Default.Encode(value);
        }

        var normalizedQuery = query.Trim();
        var encodedValue = HtmlEncoder.Default.Encode(value);
        var regex = new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(normalizedQuery), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return regex.Replace(encodedValue, match => $"<mark>{match.Value}</mark>");
    }
}

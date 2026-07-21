using System.Text.Json.Serialization;

namespace FamilyHub.Models;

/// <summary>
/// Represents the statistics and summary data shown on the dashboard.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// Gets or sets the total number of family members stored in the database.
    /// </summary>
    public int TotalFamilyMembers { get; set; }

    /// <summary>
    /// Gets or sets the total number of male family members.
    /// </summary>
    public int TotalMales { get; set; }

    /// <summary>
    /// Gets or sets the total number of female family members.
    /// </summary>
    public int TotalFemales { get; set; }

    /// <summary>
    /// Gets or sets the number of married family members.
    /// </summary>
    public int MarriedMembers { get; set; }

    /// <summary>
    /// Gets or sets the number of single family members.
    /// </summary>
    public int SingleMembers { get; set; }

    /// <summary>
    /// Gets or sets the average age of all members with a date of birth.
    /// </summary>
    public double AverageAge { get; set; }

    /// <summary>
    /// Gets or sets the name of the youngest family member.
    /// </summary>
    public string YoungestMember { get; set; } = "No members yet";

    /// <summary>
    /// Gets or sets the name of the oldest family member.
    /// </summary>
    public string OldestMember { get; set; } = "No members yet";

    /// <summary>
    /// Gets or sets the list of upcoming birthdays for the next 30 days.
    /// </summary>
    public IReadOnlyList<UpcomingBirthdayViewModel> UpcomingBirthdays { get; set; } = Array.Empty<UpcomingBirthdayViewModel>();

    /// <summary>
    /// Gets or sets the most recently added family members.
    /// </summary>
    public IReadOnlyList<RecentMemberViewModel> RecentMembers { get; set; } = Array.Empty<RecentMemberViewModel>();

    /// <summary>
    /// Gets or sets the total number of registered users.
    /// </summary>
    public int TotalRegisteredUsers { get; set; }

    /// <summary>
    /// Gets or sets the total number of stored relationships.
    /// </summary>
    public int TotalRelationships { get; set; }

    /// <summary>
    /// Gets or sets the dashboard search term.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search results for the dashboard search box.
    /// </summary>
    public IReadOnlyList<DashboardSearchResultViewModel> SearchResults { get; set; } = Array.Empty<DashboardSearchResultViewModel>();

    /// <summary>
    /// Gets or sets the recent administrative activity entries.
    /// </summary>
    public IReadOnlyList<DashboardActivityViewModel> RecentActivities { get; set; } = Array.Empty<DashboardActivityViewModel>();

    /// <summary>
    /// Gets or sets the most recent notifications for the dashboard.
    /// </summary>
    public IReadOnlyList<DashboardNotificationViewModel> RecentNotifications { get; set; } = Array.Empty<DashboardNotificationViewModel>();

    /// <summary>
    /// Gets or sets the series data for the gender chart.
    /// </summary>
    public string GenderChartDataJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the series data for the nationality chart.
    /// </summary>
    public string NationalityChartDataJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the series data for the state chart.
    /// </summary>
    public string StateChartDataJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the series data for the age group chart.
    /// </summary>
    public string AgeGroupChartDataJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the series data for the relationship chart.
    /// </summary>
    public string RelationshipChartDataJson { get; set; } = "[]";

    /// <summary>
    /// Gets a value indicating whether any family members exist.
    /// </summary>
    public bool HasMembers => TotalFamilyMembers > 0;
}

/// <summary>
/// Represents one upcoming birthday card item shown on the dashboard.
/// </summary>
public class UpcomingBirthdayViewModel
{
    /// <summary>
    /// Gets or sets the full name of the family member.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile image path for the member.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the date of birth for the member.
    /// </summary>
    public DateTime Birthday { get; set; }

    /// <summary>
    /// Gets or sets the number of days until the next birthday.
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Gets or sets the age the person will turn on their next birthday.
    /// </summary>
    public int AgeTurning { get; set; }
}

/// <summary>
/// Represents one recent family member item shown on the dashboard.
/// </summary>
public class RecentMemberViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the family member.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the family member.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the occupation of the family member.
    /// </summary>
    public string? Occupation { get; set; }

    /// <summary>
    /// Gets or sets the relationship label of the family member.
    /// </summary>
    public string? Relationship { get; set; }

    /// <summary>
    /// Gets or sets the profile image path of the family member.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the date when the member was added.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents one recent activity item shown on the dashboard.
/// </summary>
public class DashboardActivityViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string Icon { get; set; } = "bi-journal-text";

    public DateTime Timestamp { get; set; }

    public string? UserName { get; set; }
}

/// <summary>
/// Represents a search result entry for the dashboard search box.
/// </summary>
public class DashboardSearchResultViewModel
{
    public string Section { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? ImagePath { get; set; }
}

public class ChartDataPoint
{
    public ChartDataPoint(string label, int count)
    {
        Label = label;
        Count = count;
    }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

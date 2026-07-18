namespace FamilyHub.Models;

/// <summary>
/// Represents the statistics and summary data shown on the dashboard.
/// This view model keeps the home page simple and beginner friendly.
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

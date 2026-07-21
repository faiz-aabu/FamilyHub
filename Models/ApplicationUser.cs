using Microsoft.AspNetCore.Identity;

namespace FamilyHub.Models;

/// <summary>
/// Custom user entity for ASP.NET Core Identity.
/// This adds a friendly display name for the signed-in user.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the full name shown on the account pages.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the path to the user's uploaded profile picture.
    /// </summary>
    public string? ProfilePicturePath { get; set; }

    /// <summary>
    /// Gets or sets the most recent successful login timestamp.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets whether the user prefers dark mode.
    /// </summary>
    public bool DarkModePreference { get; set; }

    /// <summary>
    /// Gets or sets whether the user wants to receive in-app notifications.
    /// </summary>
    public bool NotificationPreference { get; set; } = true;

    /// <summary>
    /// Gets or sets the user's preferred UI language.
    /// </summary>
    public string PreferredLanguage { get; set; } = "English";
}

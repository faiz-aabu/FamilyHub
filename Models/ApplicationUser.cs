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
}

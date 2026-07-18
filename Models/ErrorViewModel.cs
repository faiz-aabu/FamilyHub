namespace FamilyHub.Models;

/// <summary>
/// Represents the information shown on the error page.
/// </summary>
public class ErrorViewModel
{
    /// <summary>
    /// Gets or sets the request identifier used for tracing errors.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the request ID should be shown to the user.
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

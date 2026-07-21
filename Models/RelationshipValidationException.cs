namespace FamilyHub.Models;

/// <summary>
/// Represents a domain validation error raised while saving or updating a family relationship.
/// </summary>
public class RelationshipValidationException : InvalidOperationException
{
    /// <summary>
    /// Creates a new relationship validation exception.
    /// </summary>
    /// <param name="message">The validation message.</param>
    /// <param name="propertyName">The form field associated with the error, if any.</param>
    public RelationshipValidationException(string message, string? propertyName = null)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets the form field that should be highlighted when the error is displayed.
    /// </summary>
    public string? PropertyName { get; }
}

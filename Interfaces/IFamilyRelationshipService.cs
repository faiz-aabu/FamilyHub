using FamilyHub.Models;

namespace FamilyHub.Interfaces;

/// <summary>
/// Defines the operations used to work with family relationship records.
/// </summary>
public interface IFamilyRelationshipService
{
    /// <summary>
    /// Returns every relationship record from the database.
    /// </summary>
    IEnumerable<FamilyRelationship> GetAll();

    /// <summary>
    /// Finds one relationship by its unique identifier.
    /// </summary>
    Task<FamilyRelationship?> GetByIdAsync(int id, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Creates a new relationship record.
    /// </summary>
    Task<FamilyRelationship> CreateAsync(FamilyRelationship relationship, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Updates an existing relationship record.
    /// </summary>
    Task<FamilyRelationship> UpdateAsync(FamilyRelationship relationship, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Deletes a relationship record.
    /// </summary>
    Task DeleteAsync(int id, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Searches relationships by member name, related member name, or relationship type.
    /// </summary>
    IEnumerable<FamilyRelationship> Search(string? searchTerm, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Returns all relationships that belong to a specific family member.
    /// </summary>
    IEnumerable<FamilyRelationship> GetByMemberId(int memberId);

    /// <summary>
    /// Checks whether deleting a family member would leave the relationship graph inconsistent.
    /// </summary>
    Task<(bool CanDelete, string Message)> GetDeleteImpactAsync(int memberId);
}

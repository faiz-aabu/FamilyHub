using FamilyHub.Models;

namespace FamilyHub.Interfaces;

/// <summary>
/// Defines the operations that can be used to work with family member data.
/// </summary>
public interface IFamilyMemberService
{
    /// <summary>
    /// Returns all family members that are currently available.
    /// </summary>
    /// <returns>A list of family members.</returns>
    IEnumerable<FamilyMember> GetAllFamilyMembers();

    /// <summary>
    /// Returns all family members from the data source.
    /// This is a placeholder for future CRUD work.
    /// </summary>
    /// <param name="userId">The current user identifier used to scope results.</param>
    /// <param name="isAdmin">Whether the calling user has administrator privileges.</param>
    /// <returns>A list of family members.</returns>
    IEnumerable<FamilyMember> GetAll(string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Finds one family member by its unique identifier.
    /// </summary>
    /// <param name="id">The identifier to search for.</param>
    /// <param name="userId">The current user identifier used to scope the row.</param>
    /// <param name="isAdmin">Whether the calling user has administrator privileges.</param>
    /// <returns>The matching family member, if any.</returns>
    Task<FamilyMember?> GetByIdAsync(int id, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Creates a new family member record.
    /// </summary>
    /// <param name="familyMember">The new family member to create.</param>
    /// <param name="userId">The current user identifier that should own the record.</param>
    /// <returns>The created family member.</returns>
    Task<FamilyMember> CreateAsync(FamilyMember familyMember, string? userId = null);

    /// <summary>
    /// Updates an existing family member record asynchronously.
    /// </summary>
    /// <param name="familyMember">The updated family member.</param>
    /// <param name="userId">The current user identifier used to validate ownership.</param>
    /// <param name="isAdmin">Whether the calling user has administrator privileges.</param>
    /// <returns>The updated family member.</returns>
    Task<FamilyMember> UpdateAsync(FamilyMember familyMember, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Deletes a family member record by identifier.
    /// </summary>
    /// <param name="id">The identifier to delete.</param>
    /// <param name="userId">The current user identifier used to validate ownership.</param>
    /// <param name="isAdmin">Whether the calling user has administrator privileges.</param>
    Task DeleteAsync(int id, string? userId = null, bool isAdmin = false);

    /// <summary>
    /// Searches family members by a text value using an asynchronous EF Core query.
    /// </summary>
    /// <param name="searchTerm">The search text to match.</param>
    /// <param name="userId">The current user identifier used to scope results.</param>
    /// <param name="isAdmin">Whether the calling user has administrator privileges.</param>
    /// <returns>A list of matching family members.</returns>
    Task<IEnumerable<FamilyMember>> Search(string? searchTerm, string? userId = null, bool isAdmin = false);
}

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
    /// <returns>A list of family members.</returns>
    IEnumerable<FamilyMember> GetAll();

    /// <summary>
    /// Finds one family member by its unique identifier.
    /// </summary>
    /// <param name="id">The identifier to search for.</param>
    /// <returns>The matching family member, if any.</returns>
    Task<FamilyMember?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new family member record.
    /// </summary>
    /// <param name="familyMember">The new family member to create.</param>
    /// <returns>The created family member.</returns>
    Task<FamilyMember> CreateAsync(FamilyMember familyMember);

    /// <summary>
    /// Updates an existing family member record asynchronously.
    /// </summary>
    /// <param name="familyMember">The updated family member.</param>
    /// <returns>The updated family member.</returns>
    Task<FamilyMember> UpdateAsync(FamilyMember familyMember);

    /// <summary>
    /// Deletes a family member record by identifier.
    /// </summary>
    /// <param name="id">The identifier to delete.</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// Searches family members by a text value using an asynchronous EF Core query.
    /// </summary>
    /// <param name="searchTerm">The search text to match.</param>
    /// <returns>A list of matching family members.</returns>
    Task<IEnumerable<FamilyMember>> Search(string? searchTerm);
}

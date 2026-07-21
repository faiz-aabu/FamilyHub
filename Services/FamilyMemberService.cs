using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Services;

/// <summary>
/// Provides simple family member operations for the application.
/// This service is intentionally beginner friendly and easy to extend later.
/// </summary>
public class FamilyMemberService : IFamilyMemberService
{
    private readonly FamilyHubDbContext _context;

    /// <summary>
    /// Creates a new instance of the family member service.
    /// </summary>
    /// <param name="context">The database context used to read family members.</param>
    public FamilyMemberService(FamilyHubDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all family members from the database.
    /// </summary>
    /// <returns>A collection of family members.</returns>
    public IEnumerable<FamilyMember> GetAllFamilyMembers()
    {
        return GetAll();
    }

    /// <summary>
    /// Returns all family members from the database.
    /// </summary>
    /// <returns>A list of family members.</returns>
    public IEnumerable<FamilyMember> GetAll()
    {
        return _context.FamilyMembers
            .AsNoTracking()
            .Include(x => x.RelatedFamilyMember)
            .Include(x => x.Relationships)
                .ThenInclude(x => x.RelatedMember)
            .OrderBy(x => x.FirstName)
            .ToList();
    }

    /// <summary>
    /// Finds one family member by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <returns>The matching family member.</returns>
    public async Task<FamilyMember?> GetByIdAsync(int id)
    {
        return await _context.FamilyMembers
            .AsNoTracking()
            .Include(x => x.RelatedFamilyMember)
            .Include(x => x.Relationships)
                .ThenInclude(x => x.RelatedMember)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Creates a new family member record.
    /// </summary>
    /// <param name="familyMember">The new family member to create.</param>
    /// <returns>The created family member.</returns>
    public async Task<FamilyMember> CreateAsync(FamilyMember familyMember)
    {
        // Add the new family member to the database and save changes.
        _context.FamilyMembers.Add(familyMember);
        await _context.SaveChangesAsync();
        return familyMember;
    }

    /// <summary>
    /// Updates an existing family member record in the database.
    /// </summary>
    /// <param name="familyMember">The updated family member.</param>
    /// <returns>The updated family member.</returns>
    public async Task<FamilyMember> UpdateAsync(FamilyMember familyMember)
    {
        var existingFamilyMember = await _context.FamilyMembers.FindAsync(familyMember.Id);

        if (existingFamilyMember is null)
        {
            throw new InvalidOperationException("The requested family member was not found.");
        }

        _context.Entry(existingFamilyMember).CurrentValues.SetValues(familyMember);
        existingFamilyMember.UpdatedAt = familyMember.UpdatedAt;
        await _context.SaveChangesAsync();
        return existingFamilyMember;
    }

    /// <summary>
    /// Deletes a family member record by identifier.
    /// </summary>
    /// <param name="id">The identifier to delete.</param>
    public async Task DeleteAsync(int id)
    {
        var familyMember = await _context.FamilyMembers.FindAsync(id);

        if (familyMember is null)
        {
            return;
        }

        _context.FamilyMembers.Remove(familyMember);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Searches for family members that match a text value.
    /// The search is trimmed, case-insensitive, and uses partial matching so a single input can find data across many profile fields.
    /// </summary>
    /// <param name="searchTerm">The search text.</param>
    /// <returns>A list of matching family members.</returns>
    public async Task<IEnumerable<FamilyMember>> Search(string? searchTerm)
    {
        // If the user leaves the box empty, return the full list without applying any filter.
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.FamilyMembers
                .AsNoTracking()
                .OrderBy(member => member.FirstName)
                .ThenBy(member => member.LastName)
                .ToListAsync();
        }

        // Trim the input once and normalize it so the same logic works for every field and every search term.
        var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();

        // Keep the filtering in the database layer and only materialize the matching rows after the predicate has been applied.
        var query = _context.FamilyMembers
            .AsNoTracking()
            .Where(member =>
                member.Id.ToString().Contains(searchTerm.Trim())
                || (member.FirstName != null && member.FirstName.ToLower().Contains(normalizedSearchTerm))
                || (member.LastName != null && member.LastName.ToLower().Contains(normalizedSearchTerm))
                || (member.FirstName + " " + member.LastName).ToLower().Contains(normalizedSearchTerm)
                || (member.Email != null && member.Email.ToLower().Contains(normalizedSearchTerm))
                || (member.PhoneNumber != null && member.PhoneNumber.ToLower().Contains(normalizedSearchTerm))
                || (member.Gender != null && member.Gender.ToLower().Contains(normalizedSearchTerm))
                || (member.Occupation != null && member.Occupation.ToLower().Contains(normalizedSearchTerm))
                || (member.School != null && member.School.ToLower().Contains(normalizedSearchTerm))
                || (member.Address != null && member.Address.ToLower().Contains(normalizedSearchTerm))
                || (member.State != null && member.State.ToLower().Contains(normalizedSearchTerm))
                || (member.Nationality != null && member.Nationality.ToLower().Contains(normalizedSearchTerm))
                || (member.BloodGroup != null && member.BloodGroup.ToLower().Contains(normalizedSearchTerm))
                || (member.Genotype != null && member.Genotype.ToLower().Contains(normalizedSearchTerm))
                || (member.Allergies != null && member.Allergies.ToLower().Contains(normalizedSearchTerm))
                || (member.Hobbies != null && member.Hobbies.ToLower().Contains(normalizedSearchTerm))
                || (member.Biography != null && member.Biography.ToLower().Contains(normalizedSearchTerm))
                || (member.MaritalStatus != null && member.MaritalStatus.ToLower().Contains(normalizedSearchTerm))
                || (member.Relationship != null && member.Relationship.ToLower().Contains(normalizedSearchTerm))
                || (member.Notes != null && member.Notes.ToLower().Contains(normalizedSearchTerm)));

        return await query
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();
    }
}

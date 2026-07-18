using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Services;

/// <summary>
/// Provides the data services for family relationship records.
/// </summary>
public class FamilyRelationshipService : IFamilyRelationshipService
{
    private readonly FamilyHubDbContext _context;

    /// <summary>
    /// Creates a new relationship service instance.
    /// </summary>
    public FamilyRelationshipService(FamilyHubDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns every relationship including its related navigation data.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetAll()
    {
        return _context.FamilyRelationships
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .OrderBy(x => x.Member!.FirstName)
            .ThenBy(x => x.RelatedMember!.FirstName)
            .ToList();
    }

    /// <summary>
    /// Finds a relationship by identifier.
    /// </summary>
    public async Task<FamilyRelationship?> GetByIdAsync(int id)
    {
        return await _context.FamilyRelationships
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    /// Creates a new relationship record.
    /// </summary>
    public async Task<FamilyRelationship> CreateAsync(FamilyRelationship relationship)
    {
        _context.FamilyRelationships.Add(relationship);
        await _context.SaveChangesAsync();
        return relationship;
    }

    /// <summary>
    /// Updates an existing relationship record.
    /// </summary>
    public async Task<FamilyRelationship> UpdateAsync(FamilyRelationship relationship)
    {
        _context.FamilyRelationships.Update(relationship);
        await _context.SaveChangesAsync();
        return relationship;
    }

    /// <summary>
    /// Deletes a relationship record.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var relationship = await _context.FamilyRelationships.FindAsync(id);
        if (relationship is null)
        {
            return;
        }

        _context.FamilyRelationships.Remove(relationship);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Searches relationships by member name, related member name, or relationship type.
    /// </summary>
    public IEnumerable<FamilyRelationship> Search(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetAll();
        }

        var normalizedTerm = searchTerm.Trim().ToLowerInvariant();

        return _context.FamilyRelationships
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .AsEnumerable()
            .Where(relationship =>
                (relationship.Member?.FirstName + " " + relationship.Member?.LastName).ToLowerInvariant().Contains(normalizedTerm)
                || (relationship.RelatedMember?.FirstName + " " + relationship.RelatedMember?.LastName).ToLowerInvariant().Contains(normalizedTerm)
                || relationship.RelationshipType.ToLowerInvariant().Contains(normalizedTerm))
            .OrderBy(relationship => relationship.Member?.FirstName)
            .ThenBy(relationship => relationship.RelatedMember?.FirstName)
            .ToList();
    }

    /// <summary>
    /// Returns all relationships for one family member.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetByMemberId(int memberId)
    {
        return _context.FamilyRelationships
            .Where(x => x.MemberId == memberId)
            .Include(x => x.RelatedMember)
            .OrderBy(x => x.RelationshipType)
            .ToList();
    }
}

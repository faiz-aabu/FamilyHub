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
    /// Returns every relationship record from the database.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetAll()
    {
        return GetAll(null, false);
    }

    /// <summary>
    /// Returns every relationship including its related navigation data.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetAll(string? userId = null, bool isAdmin = false)
    {
        var query = _context.FamilyRelationships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .OrderBy(x => x.Member!.FirstName)
            .ThenBy(x => x.RelatedMember!.FirstName)
            .AsQueryable();

        return ApplyUserFilter(query, userId, isAdmin).ToList();
    }

    /// <summary>
    /// Finds a relationship by identifier.
    /// </summary>
    public async Task<FamilyRelationship?> GetByIdAsync(int id, string? userId = null, bool isAdmin = false)
    {
        var query = _context.FamilyRelationships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .Where(x => x.Id == id);

        return await ApplyUserFilter(query, userId, isAdmin).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Creates a new relationship record.
    /// </summary>
    public async Task<FamilyRelationship> CreateAsync(FamilyRelationship relationship, string? userId = null, bool isAdmin = false)
    {
        await ValidateRelationshipOwnershipAsync(relationship, userId, isAdmin);
        await ValidateRelationshipAsync(relationship);
        _context.FamilyRelationships.Add(relationship);
        await _context.SaveChangesAsync();
        return relationship;
    }

    /// <summary>
    /// Updates an existing relationship record.
    /// </summary>
    public async Task<FamilyRelationship> UpdateAsync(FamilyRelationship relationship, string? userId = null, bool isAdmin = false)
    {
        await ValidateRelationshipOwnershipAsync(relationship, userId, isAdmin);
        await ValidateRelationshipAsync(relationship, relationship.Id);
        _context.FamilyRelationships.Update(relationship);
        await _context.SaveChangesAsync();
        return relationship;
    }

    /// <summary>
    /// Deletes a relationship record.
    /// </summary>
    public async Task DeleteAsync(int id, string? userId = null, bool isAdmin = false)
    {
        var query = _context.FamilyRelationships.AsQueryable();
        query = ApplyUserFilter(query, userId, isAdmin);

        var relationship = await query.FirstOrDefaultAsync(x => x.Id == id);
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
    public IEnumerable<FamilyRelationship> Search(string? searchTerm, string? userId = null, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return GetAll(userId, isAdmin);
        }

        var normalizedTerm = searchTerm.Trim();
        var query = _context.FamilyRelationships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .Where(relationship =>
                (relationship.Member != null && (relationship.Member.FirstName != null && relationship.Member.FirstName.Contains(normalizedTerm)))
                || (relationship.RelatedMember != null && (relationship.RelatedMember.FirstName != null && relationship.RelatedMember.FirstName.Contains(normalizedTerm)))
                || (relationship.RelationshipType != null && relationship.RelationshipType.Contains(normalizedTerm)))
            .AsQueryable();

        return ApplyUserFilter(query, userId, isAdmin)
            .OrderBy(relationship => relationship.Member != null ? relationship.Member.FirstName : string.Empty)
            .ThenBy(relationship => relationship.RelatedMember != null ? relationship.RelatedMember.FirstName : string.Empty)
            .ToList();
    }

    /// <summary>
    /// Returns all relationships for one family member.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetByMemberId(int memberId)
    {
        return GetByMemberId(memberId, null, false);
    }

    /// <summary>
    /// Returns all relationships for one family member.
    /// </summary>
    public IEnumerable<FamilyRelationship> GetByMemberId(int memberId, string? userId = null, bool isAdmin = false)
    {
        var query = _context.FamilyRelationships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.RelatedMember)
            .Where(x => x.MemberId == memberId || x.RelatedMemberId == memberId)
            .AsQueryable();

        return ApplyUserFilter(query, userId, isAdmin)
            .OrderBy(x => x.RelationshipType)
            .ToList();
    }

    /// <summary>
    /// Determines whether a family member can be safely deleted without leaving unresolved relationships behind.
    /// </summary>
    public async Task<(bool CanDelete, string Message)> GetDeleteImpactAsync(int memberId)
    {
        var relationshipCount = await _context.FamilyRelationships
            .AsNoTracking()
            .CountAsync(x => x.MemberId == memberId || x.RelatedMemberId == memberId);

        if (relationshipCount == 0)
        {
            return (true, "No related family relationships are attached to this member.");
        }

        return (false, $"Deleting this member would affect {relationshipCount} existing relationship record(s). Remove or reassign them first so the family tree stays consistent.");
    }

    private async Task ValidateRelationshipAsync(FamilyRelationship relationship, int? currentRelationshipId = null)
    {
        if (relationship is null)
        {
            throw new ArgumentNullException(nameof(relationship));
        }

        if (relationship.MemberId == relationship.RelatedMemberId)
        {
            throw new RelationshipValidationException("A person cannot be related to themselves.", nameof(relationship.MemberId));
        }

        var normalizedType = NormalizeRelationshipType(relationship.RelationshipType);
        if (string.IsNullOrWhiteSpace(normalizedType))
        {
            throw new RelationshipValidationException("Please choose a relationship type.", nameof(relationship.RelationshipType));
        }

        if (!FamilyRelationship.SupportedRelationshipTypes.Any(type => string.Equals(type, normalizedType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new RelationshipValidationException("Please choose a supported relationship type.", nameof(relationship.RelationshipType));
        }

        // Perform duplicate check with EF-translatable predicates (avoid StringComparison and helper calls inside the expression)
        var lowerType = normalizedType.ToLower();
        var isSymmetric = IsSymmetricRelationshipType(relationship.RelationshipType);
        bool hasDuplicateRelationship;
        if (isSymmetric)
        {
            hasDuplicateRelationship = await _context.FamilyRelationships
                .AsNoTracking()
                .AnyAsync(x => x.Id != currentRelationshipId
                    && x.RelationshipType != null && x.RelationshipType.ToLower() == lowerType
                    && ((x.MemberId == relationship.MemberId && x.RelatedMemberId == relationship.RelatedMemberId)
                        || (x.MemberId == relationship.RelatedMemberId && x.RelatedMemberId == relationship.MemberId)));
        }
        else
        {
            hasDuplicateRelationship = await _context.FamilyRelationships
                .AsNoTracking()
                .AnyAsync(x => x.Id != currentRelationshipId
                    && x.RelationshipType != null && x.RelationshipType.ToLower() == lowerType
                    && x.MemberId == relationship.MemberId
                    && x.RelatedMemberId == relationship.RelatedMemberId);
        }

        if (hasDuplicateRelationship)
        {
            throw new RelationshipValidationException("This relationship already exists.", nameof(relationship.RelationshipType));
        }

        if (IsSpouseRelationship(normalizedType))
        {
            var spouseConflict = await _context.FamilyRelationships
                .AnyAsync(x => x.Id != currentRelationshipId
                    && x.RelationshipType != null && x.RelationshipType.ToLower() == "spouse"
                    && (x.MemberId == relationship.MemberId || x.RelatedMemberId == relationship.MemberId || x.MemberId == relationship.RelatedMemberId || x.RelatedMemberId == relationship.RelatedMemberId));

            if (spouseConflict)
            {
                throw new RelationshipValidationException("This person already has an active spouse relationship.", nameof(relationship.RelationshipType));
            }
        }

        if (WouldCreateCircularRelationship(relationship, currentRelationshipId))
        {
            throw new RelationshipValidationException("This relationship would create a circular family connection.", nameof(relationship.RelationshipType));
        }

        await ValidateAgeCompatibilityAsync(relationship);
    }

    private IQueryable<FamilyRelationship> ApplyUserFilter(IQueryable<FamilyRelationship> query, string? userId, bool isAdmin)
    {
        if (isAdmin || string.IsNullOrWhiteSpace(userId))
        {
            return query;
        }

        return query.Where(relationship =>
            (relationship.Member != null && relationship.Member.UserId == userId)
            || (relationship.RelatedMember != null && relationship.RelatedMember.UserId == userId));
    }

    private async Task ValidateRelationshipOwnershipAsync(FamilyRelationship relationship, string? userId, bool isAdmin)
    {
        if (isAdmin || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var members = await _context.FamilyMembers
            .AsNoTracking()
            .Where(x => x.Id == relationship.MemberId || x.Id == relationship.RelatedMemberId)
            .ToListAsync();

        if (members.Count != 2 || members.Any(member => !string.Equals(member.UserId, userId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage relationships for one or more selected family members.");
        }
    }

    private bool WouldCreateCircularRelationship(FamilyRelationship relationship, int? currentRelationshipId)
    {
        if (!IsAncestryRelationshipType(relationship.RelationshipType))
        {
            return false;
        }

        var existingRelationships = _context.FamilyRelationships
            .AsNoTracking()
            .Where(x => x.Id != currentRelationshipId && IsAncestryRelationshipType(x.RelationshipType))
            .ToList();

        var adjacency = new Dictionary<int, List<int>>();
        foreach (var existingRelationship in existingRelationships)
        {
            var edge = GetAncestryEdge(existingRelationship);
            if (edge.Start == 0 || edge.End == 0 || edge.Start == edge.End)
            {
                continue;
            }

            if (!adjacency.TryGetValue(edge.Start, out var neighbors))
            {
                neighbors = new List<int>();
                adjacency[edge.Start] = neighbors;
            }

            neighbors.Add(edge.End);
        }

        var newEdge = GetAncestryEdge(relationship);
        if (newEdge.Start == 0 || newEdge.End == 0 || newEdge.Start == newEdge.End)
        {
            return false;
        }

        return IsReachable(newEdge.End, newEdge.Start, adjacency, new HashSet<int>());
    }

    private async Task ValidateAgeCompatibilityAsync(FamilyRelationship relationship)
    {
        var membersById = await _context.FamilyMembers
            .AsNoTracking()
            .Where(x => x.Id == relationship.MemberId || x.Id == relationship.RelatedMemberId)
            .ToDictionaryAsync(x => x.Id);

        if (!membersById.TryGetValue(relationship.MemberId, out var member) || !membersById.TryGetValue(relationship.RelatedMemberId, out var relatedMember))
        {
            return;
        }

        if (member.DateOfBirth is not { } memberDateOfBirth || relatedMember.DateOfBirth is not { } relatedDateOfBirth)
        {
            return;
        }

        switch (NormalizeRelationshipType(relationship.RelationshipType))
        {
            case "Father":
            case "Mother":
            case "Guardian":
                if (memberDateOfBirth >= relatedDateOfBirth)
                {
                    throw new RelationshipValidationException("A parent or guardian must be older than the child.", nameof(relationship.RelationshipType));
                }
                break;
            case "Son":
            case "Daughter":
                if (memberDateOfBirth <= relatedDateOfBirth)
                {
                    throw new RelationshipValidationException("A child must be younger than the parent.", nameof(relationship.RelationshipType));
                }
                break;
            case "Grandfather":
            case "Grandmother":
                if (memberDateOfBirth >= relatedDateOfBirth)
                {
                    throw new RelationshipValidationException("A grandparent must be older than the grandchild.", nameof(relationship.RelationshipType));
                }
                break;
            case "Grandchild":
                if (memberDateOfBirth <= relatedDateOfBirth)
                {
                    throw new RelationshipValidationException("A grandchild must be younger than the grandparent.", nameof(relationship.RelationshipType));
                }
                break;
        }
    }

    private static string NormalizeRelationshipType(string? relationshipType)
    {
        return string.IsNullOrWhiteSpace(relationshipType) ? string.Empty : relationshipType.Trim();
    }

    private static bool IsSpouseRelationship(string relationshipType)
    {
        return string.Equals(relationshipType, "Spouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameRelationshipPair(FamilyRelationship relationship, FamilyRelationship existingRelationship)
    {
        if (IsSymmetricRelationshipType(relationship.RelationshipType))
        {
            return (relationship.MemberId == existingRelationship.MemberId && relationship.RelatedMemberId == existingRelationship.RelatedMemberId)
                || (relationship.MemberId == existingRelationship.RelatedMemberId && relationship.RelatedMemberId == existingRelationship.MemberId);
        }

        return relationship.MemberId == existingRelationship.MemberId
            && relationship.RelatedMemberId == existingRelationship.RelatedMemberId;
    }

    private static bool IsSymmetricRelationshipType(string relationshipType)
    {
        var normalizedType = NormalizeRelationshipType(relationshipType);
        return normalizedType is "Spouse" or "Brother" or "Sister" or "Grandfather" or "Grandmother" or "Uncle" or "Aunt" or "Cousin";
    }

    private static bool IsAncestryRelationshipType(string relationshipType)
    {
        var normalizedType = NormalizeRelationshipType(relationshipType);
        return normalizedType is "Father" or "Mother" or "Son" or "Daughter" or "Guardian" or "Grandfather" or "Grandmother" or "Grandchild";
    }

    private static (int Start, int End) GetAncestryEdge(FamilyRelationship relationship)
    {
        var normalizedType = NormalizeRelationshipType(relationship.RelationshipType);
        return normalizedType switch
        {
            "Father" or "Mother" or "Guardian" or "Grandfather" or "Grandmother" => (relationship.MemberId, relationship.RelatedMemberId),
            "Son" or "Daughter" or "Grandchild" => (relationship.RelatedMemberId, relationship.MemberId),
            _ => (0, 0)
        };
    }

    private static bool IsReachable(int start, int target, IReadOnlyDictionary<int, List<int>> adjacency, HashSet<int> visited)
    {
        if (start == target)
        {
            return true;
        }

        if (!adjacency.TryGetValue(start, out var neighbors) || !visited.Add(start))
        {
            return false;
        }

        foreach (var neighbor in neighbors)
        {
            if (IsReachable(neighbor, target, adjacency, visited))
            {
                return true;
            }
        }

        return false;
    }
}

namespace FamilyHub.Models;

public class FamilyTreeViewModel
{
    public string SearchTerm { get; set; } = string.Empty;

    public IReadOnlyList<FamilyMember> Members { get; set; } = Array.Empty<FamilyMember>();

    public IReadOnlyList<FamilyTreeLinkViewModel> Links { get; set; } = Array.Empty<FamilyTreeLinkViewModel>();
}

public class FamilyTreeLinkViewModel
{
    public int MemberId { get; set; }

    public int RelatedMemberId { get; set; }

    public string RelationshipType { get; set; } = string.Empty;
}

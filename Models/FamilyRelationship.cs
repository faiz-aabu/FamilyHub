using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyHub.Models;

/// <summary>
/// Represents a link between two family members.
/// </summary>
public class FamilyRelationship
{
    public int Id { get; set; }


    [Required(ErrorMessage = "Please choose a family member.")]
    [Display(Name = "Family Member")]
    public int MemberId { get; set; }


    [Required(ErrorMessage = "Please choose the related family member.")]
    [Display(Name = "Related Family Member")]
    public int RelatedMemberId { get; set; }


    [Required(ErrorMessage = "Please choose a relationship type.")]
    [StringLength(50)]
    [Display(Name = "Relationship Type")]
    public string RelationshipType { get; set; } = string.Empty;


    [ForeignKey(nameof(MemberId))]
    public FamilyMember? Member { get; set; }


    [ForeignKey(nameof(RelatedMemberId))]
    public FamilyMember? RelatedMember { get; set; }


    public static IReadOnlyList<string> SupportedRelationshipTypes => new[]
    {
        "Father",
        "Mother",
        "Son",
        "Daughter",
        "Brother",
        "Sister",
        "Spouse",
        "Grandfather",
        "Grandmother",
        "Grandchild",
        "Uncle",
        "Aunt",
        "Cousin",
        "Guardian",
        "Other"
    };
}
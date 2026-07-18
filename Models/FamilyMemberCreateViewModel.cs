using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FamilyHub.Models;

/// <summary>
/// View model used by the create family member form.
/// This keeps the form validation simple and beginner friendly.
/// </summary>
public class FamilyMemberCreateViewModel
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(50)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Occupation is required.")]
    [StringLength(150)]
    [Display(Name = "Occupation")]
    public string? Occupation { get; set; }

    [Required(ErrorMessage = "School is required.")]
    [StringLength(200)]
    [Display(Name = "School")]
    public string? School { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [StringLength(30)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Home address is required.")]
    [StringLength(500)]
    [Display(Name = "Home Address")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "State is required.")]
    [StringLength(100)]
    [Display(Name = "State")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Nationality is required.")]
    [StringLength(100)]
    [Display(Name = "Nationality")]
    public string? Nationality { get; set; }

    [Required(ErrorMessage = "Blood group is required.")]
    [StringLength(20)]
    [Display(Name = "Blood Group")]
    public string? BloodGroup { get; set; }

    [Required(ErrorMessage = "Genotype is required.")]
    [StringLength(20)]
    [Display(Name = "Genotype")]
    public string? Genotype { get; set; }

    [Required(ErrorMessage = "Allergies are required.")]
    [StringLength(500)]
    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }

    [Required(ErrorMessage = "Hobbies are required.")]
    [StringLength(500)]
    [Display(Name = "Hobbies")]
    public string? Hobbies { get; set; }

    [Required(ErrorMessage = "Biography is required.")]
    [StringLength(2000)]
    [Display(Name = "Biography")]
    public string? Biography { get; set; }

    [Required(ErrorMessage = "Marital status is required.")]
    [StringLength(50)]
    [Display(Name = "Marital Status")]
    public string? MaritalStatus { get; set; }

    [StringLength(50)]
    [Display(Name = "Relationship")]
    public string? Relationship { get; set; }

    [Display(Name = "Related Family Member")]
    public int? RelatedFamilyMemberId { get; set; }

    [Display(Name = "Image Upload")]
    public IFormFile? ImageFile { get; set; }
}

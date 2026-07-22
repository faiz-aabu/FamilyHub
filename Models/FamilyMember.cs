using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FamilyHub.Models;

/// <summary>
/// Represents one family member in the FamilyHub application.
/// This model stores the personal information that will eventually be displayed and edited.
/// </summary>
public class FamilyMember
{
    /// <summary>
    /// Gets or sets the unique identifier for this family member.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the first name of the family member.
    /// </summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the middle name of the family member.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Middle Name")]
    public string? MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the family member.
    /// </summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the nickname of the family member.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// Gets or sets the relationship label for this family member.
    /// </summary>
    [StringLength(50)]
    [Display(Name = "Relationship")]
    public string? Relationship { get; set; }

    /// <summary>
    /// Gets or sets the related family member identifier.
    /// </summary>
    [Display(Name = "Related Family Member")]
    public int? RelatedFamilyMemberId { get; set; }

    /// <summary>
    /// Gets or sets the related family member navigation property.
    /// </summary>
    [ForeignKey("RelatedFamilyMemberId")]
    [Display(Name = "Related Family Member")]
    public FamilyMember? RelatedFamilyMember { get; set; }

    /// <summary>
    /// Gets or sets the owner user identifier for this family member record.
    /// </summary>
    [Display(Name = "Owner")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who owns this family member record.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Gets the full display name for this family member.
    /// </summary>
    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();

    /// <summary>
    /// Gets or sets the gender of the family member.
    /// </summary>
    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(50)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the family member.
    /// </summary>
    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gets the age automatically calculated from the date of birth.
    /// This is not stored manually in the database.
    /// </summary>
    [NotMapped]
    [Display(Name = "Age")]
    public int? Age => CalculateAge(DateOfBirth);

    /// <summary>
    /// Gets or sets the phone number of the family member.
    /// </summary>
    [Required(ErrorMessage = "Phone number is required.")]
    [Phone]
    [StringLength(30)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the email address of the family member.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the home address of the family member.
    /// </summary>
    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the state of the family member.
    /// </summary>
    [Required(ErrorMessage = "State is required.")]
    [StringLength(100)]
    [Display(Name = "State")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the nationality of the family member.
    /// </summary>
    [Required(ErrorMessage = "Nationality is required.")]
    [StringLength(100)]
    [Display(Name = "Nationality")]
    public string? Nationality { get; set; }

    /// <summary>
    /// Gets or sets the occupation of the family member.
    /// </summary>
    [Required(ErrorMessage = "Occupation is required.")]
    [StringLength(150)]
    [Display(Name = "Occupation")]
    public string? Occupation { get; set; }

    /// <summary>
    /// Gets or sets the place of work of the family member.
    /// </summary>
    [StringLength(200)]
    [Display(Name = "Place of Work")]
    public string? PlaceOfWork { get; set; }

    /// <summary>
    /// Gets or sets the school of the family member.
    /// </summary>
    [Required(ErrorMessage = "School is required.")]
    [StringLength(200)]
    [Display(Name = "School")]
    public string? School { get; set; }

    /// <summary>
    /// Gets or sets the current class of the family member.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Current Class")]
    public string? CurrentClass { get; set; }

    /// <summary>
    /// Gets or sets the blood group of the family member.
    /// </summary>
    [Required(ErrorMessage = "Blood group is required.")]
    [StringLength(20)]
    [Display(Name = "Blood Group")]
    public string? BloodGroup { get; set; }

    /// <summary>
    /// Gets or sets the genotype of the family member.
    /// </summary>
    [Required(ErrorMessage = "Genotype is required.")]
    [StringLength(20)]
    [Display(Name = "Genotype")]
    public string? Genotype { get; set; }

    /// <summary>
    /// Gets or sets any allergies of the family member.
    /// </summary>
    [Required(ErrorMessage = "Allergies are required.")]
    [StringLength(500)]
    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }

    /// <summary>
    /// Gets or sets medical conditions of the family member.
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Medical Conditions")]
    public string? MedicalConditions { get; set; }

    /// <summary>
    /// Gets or sets the religion of the family member.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Religion")]
    public string? Religion { get; set; }

    /// <summary>
    /// Gets or sets the marital status of the family member.
    /// </summary>
    [Required(ErrorMessage = "Marital status is required.")]
    [StringLength(50)]
    [Display(Name = "Marital Status")]
    public string? MaritalStatus { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the family member.
    /// </summary>
    [StringLength(2000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets a short biography of the family member.
    /// </summary>
    [Required(ErrorMessage = "Biography is required.")]
    [StringLength(2000)]
    [Display(Name = "Biography")]
    public string? Biography { get; set; }

    /// <summary>
    /// Gets or sets the favourite food of the family member.
    /// </summary>
    [StringLength(150)]
    [Display(Name = "Favourite Food")]
    public string? FavouriteFood { get; set; }

    /// <summary>
    /// Gets or sets the favourite color of the family member.
    /// </summary>
    [StringLength(50)]
    [Display(Name = "Favourite Color")]
    public string? FavouriteColor { get; set; }

    /// <summary>
    /// Gets or sets the favourite quote of the family member.
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Favourite Quote")]
    public string? FavouriteQuote { get; set; }

    /// <summary>
    /// Gets or sets hobbies of the family member.
    /// </summary>
    [Required(ErrorMessage = "Hobbies are required.")]
    [StringLength(500)]
    [Display(Name = "Hobbies")]
    public string? Hobbies { get; set; }

    /// <summary>
    /// Gets or sets the image path for the family member.
    /// </summary>
    [StringLength(500)]
    [Display(Name = "Image Path")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the list of family relationships that belong to this family member.
    /// </summary>
    public ICollection<FamilyRelationship> Relationships { get; set; } = new List<FamilyRelationship>();

    /// <summary>
    /// Gets or sets the date when this record was created.
    /// </summary>
    [DataType(DataType.DateTime)]
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date when this record was last updated.
    /// </summary>
    [DataType(DataType.DateTime)]
    [Display(Name = "Updated At")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the age from the supplied date of birth.
    /// </summary>
    /// <param name="dateOfBirth">The family member's date of birth.</param>
    /// <returns>The computed age, or null if no date of birth has been supplied.</returns>
    private static int? CalculateAge(DateTime? dateOfBirth)
    {
        if (dateOfBirth is null)
        {
            return null;
        }

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}

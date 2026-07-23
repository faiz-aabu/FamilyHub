using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FamilyHub.Models;

public class ManageAccountViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a username.")]
    [StringLength(50, ErrorMessage = "The username cannot exceed 50 characters.")]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "Bio")]
    [StringLength(2000, ErrorMessage = "The bio cannot exceed {1} characters.")]
    public string? Bio { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? DeletePassword { get; set; }

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePictureFile { get; set; }

    public string? ProfilePicturePath { get; set; }

    public DateTimeOffset? AccountCreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string? CurrentRole { get; set; }

    public string? AccountStatus { get; set; }

    public bool IsAdministrator { get; set; }

    public int TotalManagedUsers { get; set; }

    public bool ConfirmDelete { get; set; }

    [Display(Name = "Enable two-factor authentication")]
    public bool EnableTwoFactor { get; set; }

    public string ActionType { get; set; } = "profile";
}

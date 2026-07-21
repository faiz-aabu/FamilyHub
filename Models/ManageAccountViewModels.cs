using System.ComponentModel.DataAnnotations;

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

    [Display(Name = "Dark Mode")]
    public bool DarkModePreference { get; set; }

    [Display(Name = "Notifications")]
    public bool NotificationPreference { get; set; } = true;

    [Required(ErrorMessage = "Please select a language.")]
    [Display(Name = "Language")]
    public string PreferredLanguage { get; set; } = "English";

    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Enable Two-Factor Authentication")]
    public bool EnableTwoFactor { get; set; }

    public string? ProfilePicturePath { get; set; }

    public DateTimeOffset? AccountCreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string? CurrentRole { get; set; }

    public IFormFile? ProfilePictureFile { get; set; }

    public bool ConfirmDelete { get; set; }

    public string ActionType { get; set; } = "profile";
}

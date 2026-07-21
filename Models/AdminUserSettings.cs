namespace FamilyHub.Models;

public class AdminUserSettings
{
    public bool SeedAdmin { get; set; } = false;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
}

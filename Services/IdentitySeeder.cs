using FamilyHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FamilyHub.Services;

public class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminUserSettings _adminUserSettings;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminUserSettings> adminUserOptions)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminUserSettings = adminUserOptions.Value;
    }

    public async Task SeedAsync()
    {
        await EnsureRoleAsync(ApplicationRoles.AdminLegacy);
        await EnsureRoleAsync(ApplicationRoles.Administrator);
        await EnsureRoleAsync(ApplicationRoles.UserLegacy);
        await EnsureRoleAsync(ApplicationRoles.Customer);

        var adminEmail = !string.IsNullOrWhiteSpace(_adminUserSettings.Email)
            ? _adminUserSettings.Email
            : "faidhullah@adminfamilyhub.com";
        var adminPassword = !string.IsNullOrWhiteSpace(_adminUserSettings.Password)
            ? _adminUserSettings.Password
            : "@ishaSule1";
        var adminFullName = !string.IsNullOrWhiteSpace(_adminUserSettings.FullName)
            ? _adminUserSettings.FullName
            : "FamilyHub Administrator";

        await EnsureDefaultAdminAsync(adminEmail, adminPassword, adminFullName);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    private async Task EnsureDefaultAdminAsync(string adminEmail, string adminPassword, string adminFullName)
    {
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = adminFullName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create default admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            adminUser.FullName = adminFullName;
            adminUser.EmailConfirmed = true;
            adminUser.UserName = adminEmail;
            adminUser.Email = adminEmail;
            await _userManager.UpdateAsync(adminUser);
        }

        var adminRoles = new[] { ApplicationRoles.AdminLegacy, ApplicationRoles.Administrator };
        var missingAdminRoles = new List<string>();

        foreach (var roleName in adminRoles)
        {
            if (!await _userManager.IsInRoleAsync(adminUser, roleName))
            {
                missingAdminRoles.Add(roleName);
            }
        }

        if (missingAdminRoles.Any())
        {
            var roleResult = await _userManager.AddToRolesAsync(adminUser, missingAdminRoles);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign administrator roles: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await _userManager.HasPasswordAsync(adminUser))
        {
            var addPasswordResult = await _userManager.AddPasswordAsync(adminUser, adminPassword);
            if (!addPasswordResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to set admin password: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
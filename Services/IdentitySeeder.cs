using FamilyHub.Models;
using Microsoft.AspNetCore.Identity;

namespace FamilyHub.Services;

public class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedAsync()
    {
        await EnsureRoleAsync("Admin");
        await EnsureRoleAsync("User");
        await EnsureDefaultAdminAsync();
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

    private async Task EnsureDefaultAdminAsync()
    {
        const string email = "admin@familyhub.com";
        const string password = "Password123!";
        const string fullName = "System Administrator";

        var adminUser = await _userManager.FindByEmailAsync(email);

        // Create the admin account if it doesn't exist
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(adminUser, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create default admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            adminUser = await _userManager.FindByEmailAsync(email);
        }

        // Always make sure the Admin role is assigned
        if (!await _userManager.IsInRoleAsync(adminUser!, "Admin"))
        {
            var roleResult = await _userManager.AddToRoleAsync(adminUser!, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        // Always ensure the admin password matches the expected value.
        if (adminUser is not null)
        {
            var hasPassword = await _userManager.HasPasswordAsync(adminUser);
            if (hasPassword)
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(adminUser);
                if (!removePasswordResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to reset admin password: {string.Join(", ", removePasswordResult.Errors.Select(e => e.Description))}");
                }
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(adminUser, password);
            if (!addPasswordResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to set admin password: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
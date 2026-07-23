using System;
using System.Threading.Tasks;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FamilyHub.Areas.Identity.Pages.Account;

[Authorize]
public class ManageModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ManageModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public ManageAccountViewModel ManageAccount { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        ManageAccount = new ManageAccountViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfilePicturePath = user.ProfilePicturePath,
            CurrentRole = (await _userManager.GetRolesAsync(user)).Count > 0 ? string.Join(", ", await _userManager.GetRolesAsync(user)) : "Member",
            AccountCreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            EnableTwoFactor = await _userManager.GetTwoFactorEnabledAsync(user)
        };

        return Page();
    }
}

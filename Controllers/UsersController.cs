using System.Security.Claims;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, INotificationService notificationService, IActivityLogService activityLogService, ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToListAsync();

        var viewModel = new UsersIndexViewModel
        {
            Users = new List<UserListItemViewModel>()
        };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModel.Users.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? "-",
                Email = user.Email ?? "-",
                Role = roles.FirstOrDefault() ?? "User",
                EmailConfirmed = user.EmailConfirmed,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt?.ToLocalTime().ToString("dd MMM yyyy") ?? "-"
            });
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserDetailsViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? "-",
            Email = user.Email ?? "-",
            UserName = user.UserName ?? user.Email ?? "-",
            PhoneNumber = user.PhoneNumber ?? "-",
            Role = roles.FirstOrDefault() ?? "User",
            DateJoined = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.EmailConfirmed = model.EmailConfirmed;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Updated account information for {user.FullName ?? user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChangeRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var selectedRole = currentRoles.FirstOrDefault() ?? "User";

        var viewModel = new UserRoleViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.Email ?? "User",
            Email = user.Email ?? "-",
            SelectedRole = selectedRole,
            AvailableRoles = (await _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => r != null)
                .OrderBy(r => r)
                .ToListAsync())
                .Select(r => new SelectListItem(r!, r!)).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, UserRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id && model.SelectedRole != "Admin")
        {
            TempData["ErrorMessage"] = "You cannot remove your own Admin role.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            AddErrors(removeResult.Errors);
            return View(model);
        }

        var addResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
        if (!addResult.Succeeded)
        {
            AddErrors(addResult.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Updated the role for {user.FullName ?? user.Email}.";
        await _activityLogService.LogAsync("Role Changed", $"Changed the role for {user.FullName ?? user.Email} to {model.SelectedRole}.", "User", user.Id, true, "Role assignment updated.");

        try
        {
            await _notificationService.CreateForUserAndAdminsAsync(
                user.Id,
                "Role changed",
                $"Your account role was updated to {model.SelectedRole}.",
                Url.Action(nameof(Details), "Users", new { id = user.Id }),
                "User",
                null,
                "Information",
                "bi-person-gear");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role update notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Role update notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var viewModel = new UserResetPasswordViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.Email ?? "User",
            Email = user.Email ?? "-"
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, UserResetPasswordViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot reset your own password from this page.";
            return RedirectToAction(nameof(Index));
        }

        var tempPassword = GenerateTemporaryPassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"A temporary password was created for {user.FullName ?? user.Email}. The user must change it at the next sign-in.";
        TempData["TemporaryPassword"] = tempPassword;
        await _activityLogService.LogAsync("Password Reset", $"Reset the password for {user.FullName ?? user.Email}.", "User", user.Id, true, "Temporary password issued.");

        try
        {
            await _notificationService.CreateForUserAndAdminsAsync(
                user.Id,
                "Password reset",
                "Your password was reset. Please sign in and change it using the temporary password provided.",
                Url.Action(nameof(Details), "Users", new { id = user.Id }),
                "User",
                null,
                "Warning",
                "bi-key-fill");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Password reset notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var viewModel = new UserDeleteViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.Email ?? "User",
            Email = user.Email ?? "-"
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, UserDeleteViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Deleted the account for {user.FullName ?? user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Lock(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var viewModel = new UserLockViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.Email ?? "User",
            Email = user.Email ?? "-"
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(string id, UserLockViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Index));
        }

        var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Locked the account for {user.FullName ?? user.Email}.";
        await _activityLogService.LogAsync("Account Locked", $"Locked the account for {user.FullName ?? user.Email}.", "User", user.Id, true, "Account lockout applied.");

        try
        {
            await _notificationService.CreateForUserAndAdminsAsync(
                user.Id,
                "Account locked",
                "Your account has been locked. Please contact support for assistance.",
                Url.Action(nameof(Details), "Users", new { id = user.Id }),
                "User",
                null,
                "Warning",
                "bi-lock-fill");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account lock notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Account lock notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Unlock(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var viewModel = new UserUnlockViewModel
        {
            Id = user.Id,
            FullName = user.FullName ?? user.Email ?? "User",
            Email = user.Email ?? "-"
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id, UserUnlockViewModel model)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Unlocked the account for {user.FullName ?? user.Email}.";
        await _activityLogService.LogAsync("Account Unlocked", $"Unlocked the account for {user.FullName ?? user.Email}.", "User", user.Id, true, "Account lockout removed.");

        try
        {
            await _notificationService.CreateForUserAndAdminsAsync(
                user.Id,
                "Account unlocked",
                "Your account has been unlocked successfully.",
                Url.Action(nameof(Details), "Users", new { id = user.Id }),
                "User",
                null,
                "Success",
                "bi-unlock-fill");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account unlock notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Account unlock notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
        }
        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Temp{Guid.NewGuid():N}".Substring(0, 16);
    }
}

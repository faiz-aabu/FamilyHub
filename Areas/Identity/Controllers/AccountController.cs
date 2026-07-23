using System.IO;
using System.Security.Claims;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IActivityLogService activityLogService,
        INotificationService notificationService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            await _activityLogService.LogAsync(
                "Login",
                user is null ? "User signed in successfully." : $"User {user.FullName ?? user.Email} signed in.",
                "Account",
                user?.Id,
                true,
                "Successful sign-in.");

            if (user is not null)
            {
                user.LastLoginAt = DateTimeOffset.UtcNow;
                await _userManager.UpdateAsync(user);

                try
                {
                    await _notificationService.CreateAsync(
                        user.Id,
                        "Login successful",
                        "You signed in successfully to FamilyHub.",
                        Url.Action("Manage", "Account", new { area = "Identity" }),
                        "Account",
                        null,
                        "Success",
                        "bi-box-arrow-in-right");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Login success notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                    Console.WriteLine($"[CaughtException] Login success notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
                }
            }

            return RedirectToLocal(returnUrl);
        }

        var failedUser = await _userManager.FindByEmailAsync(model.Email);
        await _activityLogService.LogAsync(
            "Login",
            failedUser is null ? "Failed login attempt." : $"Failed login attempt for {failedUser.FullName ?? failedUser.Email}.",
            "Account",
            failedUser?.Id,
            false,
            "Invalid credentials.");

        if (failedUser is not null)
        {
            try
            {
                await _notificationService.CreateAsync(
                    failedUser.Id,
                    "Login failed",
                    "A sign-in attempt failed. Please verify your credentials and try again.",
                    Url.Action("Login", "Account", new { area = "Identity" }),
                    "Account",
                    null,
                    "Error",
                    "bi-shield-lock-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failure notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Login failure notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.Customer);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _activityLogService.LogAsync(
                "Create",
                $"User {user.FullName ?? user.Email} registered a new account.",
                "Account",
                user.Id,
                true,
                "New account registration completed.");

            try
            {
                await _notificationService.CreateForUserAndAdminsAsync(
                    user.Id,
                    "Welcome to FamilyHub",
                    "Your account was created successfully. You can start managing your family records right away.",
                    Url.Action("Manage", "Account", new { area = "Identity" }),
                    "Account",
                    null,
                    "Success",
                    "bi-person-check-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Registration notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }

            return RedirectToLocal(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [AcceptVerbs("GET", "POST")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _signInManager.SignOutAsync();
        await _activityLogService.LogAsync(
            "Logout",
            "User signed out.",
            "Account",
            userId,
            true,
            "Successful sign-out.");
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isAdministrator = roles.Any(ApplicationRoles.IsAdministratorRole);

        var model = new ManageAccountViewModel
        {
            FullName = user.FullName ?? string.Empty,
            UserName = user.UserName ?? user.Email ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Bio = user.Bio,
            ProfilePicturePath = user.ProfilePicturePath,
            AccountCreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            CurrentRole = ApplicationRoles.GetDisplayRole(roles.FirstOrDefault()),
            IsAdministrator = isAdministrator,
            EnableTwoFactor = user.TwoFactorEnabled,
            AccountStatus = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTimeOffset.UtcNow
                ? $"Locked until {user.LockoutEnd.Value.LocalDateTime:dd MMM yyyy HH:mm}"
                : "Active",
            TotalManagedUsers = isAdministrator ? await _userManager.Users.CountAsync() : 0,
            ActionType = "profile"
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(ManageAccountViewModel model, string actionType = "profile")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isAdministrator = roles.Any(ApplicationRoles.IsAdministratorRole);

        model.ActionType = string.IsNullOrWhiteSpace(actionType) ? "profile" : actionType;
        model.ProfilePicturePath = user.ProfilePicturePath;
        model.AccountCreatedAt = user.CreatedAt;
        model.CurrentRole = ApplicationRoles.GetDisplayRole(roles.FirstOrDefault());
        model.LastLoginAt = user.LastLoginAt;
        model.IsAdministrator = isAdministrator;
        model.AccountStatus = user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTimeOffset.UtcNow
            ? $"Locked until {user.LockoutEnd.Value.LocalDateTime:dd MMM yyyy HH:mm}"
            : "Active";
        model.TotalManagedUsers = isAdministrator ? await _userManager.Users.CountAsync() : 0;

        if (model.ActionType == "profile")
        {
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(nameof(model.FullName), "Please enter your full name.");
            }

            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Please enter a username.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && !string.Equals(model.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
                if (existingUser is not null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
                }
            }

            var desiredUserName = model.UserName?.Trim() ?? string.Empty;
            var existingUsername = await _userManager.FindByNameAsync(desiredUserName);
            if (existingUsername is not null && existingUsername.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.UserName), "That username is already taken.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FullName = model.FullName.Trim();
            user.UserName = desiredUserName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

            var desiredEmail = model.Email.Trim();
            var emailChanged = !string.Equals(user.Email, desiredEmail, StringComparison.OrdinalIgnoreCase);
            if (emailChanged)
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "Please enter your current password to change your email.");
                    return View(model);
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
                if (!passwordCheck)
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "The current password you entered is incorrect.");
                    return View(model);
                }

                var emailToken = await _userManager.GenerateChangeEmailTokenAsync(user, desiredEmail);
                var emailResult = await _userManager.ChangeEmailAsync(user, desiredEmail, emailToken);
                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                user.Email = desiredEmail;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Your profile information was updated successfully.";
            await _activityLogService.LogAsync(
                "Profile Updated",
                $"Updated account profile for {user.FullName ?? user.Email}.",
                "Account",
                user.Id,
                true,
                "Profile updated.");

            try
            {
                await _notificationService.CreateAsync(
                    user.Id,
                    "Profile updated",
                    "Your profile details were updated successfully.",
                    Url.Action(nameof(Manage), "Account", new { area = "Identity" }),
                    "Account",
                    null,
                    "Information",
                    "bi-person-lines-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Profile update notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }

            return RedirectToAction(nameof(Manage));
        }

        if (model.ActionType == "password")
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Please enter your current password.");
            }

            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Please enter a new password.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Please confirm your new password.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword!);
            if (!passwordCheck)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "The current password you entered is incorrect.");
                return View(model);
            }

            var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword!);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Your password was changed successfully.";
            await _activityLogService.LogAsync(
                "Update",
                $"Changed password for {user.FullName ?? user.Email}.",
                "Account",
                user.Id,
                true,
                "Password updated.");

            try
            {
                await _notificationService.CreateAsync(
                    user.Id,
                    "Password updated",
                    "Your password was changed successfully.",
                    Url.Action(nameof(Manage), "Account", new { area = "Identity" }),
                    "Account",
                    null,
                    "Success",
                    "bi-key-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Password change notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }

            return RedirectToAction(nameof(Manage));
        }

        if (model.ActionType == "picture")
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            if (model.ProfilePictureFile is not null && model.ProfilePictureFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(model.ProfilePictureFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePictureFile.CopyToAsync(stream);
                }

                user.ProfilePicturePath = "/images/profiles/" + fileName;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = "Your profile picture was updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Please choose an image file to upload.";
            }

            return RedirectToAction(nameof(Manage));
        }

        if (model.ActionType == "remove-picture")
        {
            user.ProfilePicturePath = null;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Your profile picture was removed.";
            return RedirectToAction(nameof(Manage));
        }

        if (model.ActionType == "2fa")
        {
            user.TwoFactorEnabled = model.EnableTwoFactor;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = model.EnableTwoFactor ? "Two-factor authentication is now enabled for your account." : "Two-factor authentication was disabled.";

            try
            {
                await _notificationService.CreateAsync(
                    user.Id,
                    "Security updated",
                    model.EnableTwoFactor ? "Two-factor authentication was enabled for your account." : "Two-factor authentication was disabled for your account.",
                    Url.Action(nameof(Manage), "Account", new { area = "Identity" }),
                    "Account",
                    null,
                    "Information",
                    "bi-shield-check");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Security update notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Security update notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }

            return RedirectToAction(nameof(Manage));
        }

        if (model.ActionType == "delete")
        {
            if (!model.ConfirmDelete)
            {
                ModelState.AddModelError(nameof(model.ConfirmDelete), "Please confirm account deletion before continuing.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.DeletePassword))
            {
                ModelState.AddModelError(nameof(model.DeletePassword), "Please enter your current password to delete your account.");
                return View(model);
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.DeletePassword!);
            if (!passwordCheck)
            {
                ModelState.AddModelError(nameof(model.DeletePassword), "The password entered is incorrect.");
                return View(model);
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                foreach (var error in deleteResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Your account was deleted successfully.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, "Unknown account action.");
        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Dashboard", "Home", new { area = "" });
    }
}

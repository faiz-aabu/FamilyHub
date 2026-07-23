using System.ComponentModel.DataAnnotations;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FamilyHub.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        _logger.LogInformation("Razor login POST started. Email: {Email}, ReturnUrl: {ReturnUrl}, Path: {Path}", Input.Email, ReturnUrl, Request.Path);
        Console.WriteLine($"[RazorLogin] POST started; Email={Input.Email}; ReturnUrl={ReturnUrl}; Path={Request.Path}");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
        _logger.LogInformation("Razor password sign-in completed. Succeeded: {Succeeded}, LockedOut: {LockedOut}, NotAllowed: {NotAllowed}, RequiresTwoFactor: {RequiresTwoFactor}, Email: {Email}", result.Succeeded, result.IsLockedOut, result.IsNotAllowed, result.RequiresTwoFactor, Input.Email);
        Console.WriteLine($"[RazorLogin] Password sign-in completed; Succeeded={result.Succeeded}; LockedOut={result.IsLockedOut}; NotAllowed={result.IsNotAllowed}; RequiresTwoFactor={result.RequiresTwoFactor}; Email={Input.Email}");

        if (result.Succeeded)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                var roles = user is null ? Array.Empty<string>() : await _userManager.GetRolesAsync(user);
                var userName = user?.UserName ?? "(unknown)";
                var redirectDestination = !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
                    ? ReturnUrl
                    : Url.Action("Index", "Home", new { area = "" }) ?? "/Home/Index";

                _logger.LogInformation("Razor login authenticated. UserId: {UserId}, Email: {Email}, Username: {Username}, Roles: {Roles}, RedirectDestination: {RedirectDestination}", user?.Id, user?.Email, userName, string.Join(", ", roles), redirectDestination);
                Console.WriteLine($"[RazorLogin] Authenticated; UserId={user?.Id}; Email={user?.Email}; Username={userName}; Roles={string.Join(", ", roles)}; RedirectDestination={redirectDestination}");

                _logger.LogInformation("Razor login redirect starting to Home/Index. UserId: {UserId}, Email: {Email}, Username: {Username}, Roles: {Roles}, RedirectDestination: {RedirectDestination}", user?.Id, user?.Email, userName, string.Join(", ", roles), redirectDestination);
                Console.WriteLine($"[RazorLogin] Redirect starting; UserId={user?.Id}; Email={user?.Email}; Username={userName}; Roles={string.Join(", ", roles)}; RedirectDestination={redirectDestination}");
                return RedirectToAction("Index", "Home", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razor login succeeded but post-authentication redirect preparation failed. Email: {Email}, ReturnUrl: {ReturnUrl}, Path: {Path}", Input.Email, ReturnUrl, Request.Path);
                Console.WriteLine($"[RazorLogin] Post-authentication redirect preparation failed; Email={Input.Email}; ReturnUrl={ReturnUrl}; Path={Request.Path}{Environment.NewLine}{ex}");
                throw;
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Controllers;

/// <summary>
/// Provides a simple login page placeholder for the application.
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// Shows the login page.
    /// </summary>
    /// <returns>The login view.</returns>
    public IActionResult Login()
    {
        return View();
    }
}

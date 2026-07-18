using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Controllers;

/// <summary>
/// Provides a simple search page placeholder for the application.
/// </summary>
[Authorize]
public class SearchController : Controller
{
    /// <summary>
    /// Shows the search page.
    /// </summary>
    /// <returns>The search view.</returns>
    public IActionResult Index()
    {
        return View();
    }
}

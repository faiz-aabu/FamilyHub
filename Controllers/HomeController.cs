using System.Diagnostics;
using FamilyHub.Data;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Controllers;

/// <summary>
/// Handles the main home page and error page for the application.
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly FamilyHubDbContext _context;

    /// <summary>
    /// Creates a new home controller instance.
    /// </summary>
    /// <param name="logger">The logging service used by the controller.</param>
    public HomeController(ILogger<HomeController> logger, FamilyHubDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Shows the public landing page for FamilyHub.
    /// </summary>
    /// <returns>The home page view.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        return await ShowDashboardViewAsync();
    }

    /// <summary>
    /// Shows the authenticated dashboard page.
    /// </summary>
    /// <returns>The dashboard view.</returns>
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        return await ShowDashboardViewAsync();
    }

    private async Task<IActionResult> ShowDashboardViewAsync()
    {
        // Load the members once so the dashboard can show live database statistics.
        var members = await _context.FamilyMembers
            .AsNoTracking()
            .OrderByDescending(member => member.CreatedAt)
            .ToListAsync();

        var totalRegisteredUsers = await _context.Users.CountAsync();

        var dashboardViewModel = new DashboardViewModel
        {
            TotalFamilyMembers = members.Count,
            TotalRegisteredUsers = totalRegisteredUsers,
            TotalMales = members.Count(member => string.Equals(member.Gender, "Male", StringComparison.OrdinalIgnoreCase)),
            TotalFemales = members.Count(member => string.Equals(member.Gender, "Female", StringComparison.OrdinalIgnoreCase)),
            MarriedMembers = members.Count(member => string.Equals(member.MaritalStatus, "Married", StringComparison.OrdinalIgnoreCase)),
            SingleMembers = members.Count(member => string.Equals(member.MaritalStatus, "Single", StringComparison.OrdinalIgnoreCase)),
            AverageAge = members.Where(member => member.DateOfBirth.HasValue).Select(member => member.Age ?? 0).DefaultIfEmpty(0).Average(),
            YoungestMember = members.Where(member => member.DateOfBirth.HasValue).OrderBy(member => member.DateOfBirth).Select(member => member.FirstName + " " + member.LastName).FirstOrDefault() ?? "No members yet",
            OldestMember = members.Where(member => member.DateOfBirth.HasValue).OrderByDescending(member => member.DateOfBirth).Select(member => member.FirstName + " " + member.LastName).FirstOrDefault() ?? "No members yet",
            UpcomingBirthdays = members
                .Where(member => member.DateOfBirth.HasValue)
                .Select(member => new UpcomingBirthdayViewModel
                {
                    Name = member.FirstName + " " + member.LastName,
                    ImagePath = member.ImagePath,
                    Birthday = member.DateOfBirth!.Value,
                    DaysRemaining = CalculateDaysUntilBirthday(member.DateOfBirth!.Value)
                })
                .Where(item => item.DaysRemaining >= 0 && item.DaysRemaining <= 30)
                .OrderBy(item => item.DaysRemaining)
                .Take(5)
                .ToList(),
            RecentMembers = members
                .Take(5)
                .Select(member => new RecentMemberViewModel
                {
                    Id = member.Id,
                    Name = member.FirstName + " " + member.LastName,
                    Occupation = member.Occupation,
                    Relationship = member.Relationship,
                    ImagePath = member.ImagePath,
                    CreatedAt = member.CreatedAt
                })
                .ToList()
        };

        return View("Index", dashboardViewModel);
    }

    /// <summary>
    /// Calculates the number of days until the next birthday from today.
    /// </summary>
    /// <param name="dateOfBirth">The date of birth value.</param>
    /// <returns>The number of days remaining until the next birthday.</returns>
    private static int CalculateDaysUntilBirthday(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var nextBirthday = new DateTime(today.Year, dateOfBirth.Month, dateOfBirth.Day);

        if (nextBirthday < today)
        {
            nextBirthday = nextBirthday.AddYears(1);
        }

        return (nextBirthday - today).Days;
    }

    /// <summary>
    /// Shows a simple error page when something goes wrong.
    /// </summary>
    /// <returns>An error view.</returns>
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

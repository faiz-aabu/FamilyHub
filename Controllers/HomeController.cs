using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using FamilyHub.Data;
using FamilyHub.Interfaces;
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
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Creates a new home controller instance.
    /// </summary>
    /// <param name="logger">The logging service used by the controller.</param>
    public HomeController(ILogger<HomeController> logger, FamilyHubDbContext context, INotificationService notificationService)
    {
        _logger = logger;
        _context = context;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Shows the public landing page for FamilyHub.
    /// </summary>
    /// <returns>The home page view.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        return await ExecuteDashboardWithDiagnosticsAsync(nameof(Index));
    }

    /// <summary>
    /// Shows the authenticated dashboard page.
    /// </summary>
    /// <returns>The dashboard view.</returns>
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        return await ExecuteDashboardWithDiagnosticsAsync(nameof(Dashboard));
    }

    private async Task<IActionResult> ExecuteDashboardWithDiagnosticsAsync(string actionName)
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User?.Identity?.Name;
        var roles = User?.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray() ?? Array.Empty<string>();

        _logger.LogInformation("Landing action started. Action: {Action}, UserId: {UserId}, Email: {Email}, Roles: {Roles}, Path: {Path}", actionName, userId, userEmail, string.Join(", ", roles), Request.Path);
        Console.WriteLine($"[LandingAction] Started; Action={actionName}; UserId={userId}; Email={userEmail}; Roles={string.Join(", ", roles)}; Path={Request.Path}");

        try
        {
            var result = await ShowDashboardViewAsync();
            _logger.LogInformation("Landing action completed. Action: {Action}, UserId: {UserId}, Path: {Path}", actionName, userId, Request.Path);
            Console.WriteLine($"[LandingAction] Completed; Action={actionName}; UserId={userId}; Path={Request.Path}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Landing action failed. Action: {Action}, UserId: {UserId}, Email: {Email}, Roles: {Roles}, Path: {Path}", actionName, userId, userEmail, string.Join(", ", roles), Request.Path);
            Console.WriteLine($"[LandingAction] Failed; Action={actionName}; UserId={userId}; Email={userEmail}; Roles={string.Join(", ", roles)}; Path={Request.Path}{Environment.NewLine}{ex}");
            throw;
        }
    }

    private async Task<IActionResult> ShowDashboardViewAsync()
    {
        var userId = User?.Identity?.IsAuthenticated == true ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
        var isAdmin = User?.IsInRole(ApplicationRoles.Administrator) == true || User?.IsInRole(ApplicationRoles.AdminLegacy) == true;

        var membersQuery = _context.FamilyMembers.AsNoTracking();
        if (!isAdmin)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                membersQuery = membersQuery.Where(member => false);
            }
            else
            {
                membersQuery = membersQuery.Where(member => member.UserId == userId);
            }
        }

        _logger.LogInformation("Dashboard query starting: FamilyMembers. UserId: {UserId}, IsAdmin: {IsAdmin}", userId, isAdmin);
        var members = await ExecuteDashboardQueryAsync("FamilyMembers", () => membersQuery
            .OrderByDescending(member => member.CreatedAt)
            .ToListAsync());

        var totalRegisteredUsers = isAdmin
            ? await ExecuteDashboardQueryAsync("AspNetUsers count", () => _context.Users.AsNoTracking().CountAsync())
            : (string.IsNullOrWhiteSpace(userId) ? 0 : 1);

        var relationshipQuery = _context.FamilyRelationships.AsNoTracking();
        if (!isAdmin)
        {
            relationshipQuery = relationshipQuery
                .Where(relation => (relation.Member != null && relation.Member.UserId == userId)
                    || (relation.RelatedMember != null && relation.RelatedMember.UserId == userId));
        }

        var totalRelationships = await ExecuteDashboardQueryAsync("FamilyRelationships count", () => relationshipQuery.CountAsync());

        var recentActivitiesQuery = _context.ActivityLogs.AsNoTracking();
        if (!isAdmin)
        {
            recentActivitiesQuery = recentActivitiesQuery.Where(log => log.UserId == userId);
        }

        var recentActivities = await ExecuteDashboardQueryAsync("ActivityLogs recent", () => recentActivitiesQuery
            .OrderByDescending(log => log.Timestamp)
            .Take(8)
            .Select(log => new DashboardActivityViewModel
            {
                Title = log.Action,
                Detail = log.Description,
                Icon = ResolveActivityIcon(log.Action),
                Timestamp = log.Timestamp,
                UserName = log.UserName ?? "System"
            })
            .ToListAsync());

        var relationshipItems = await ExecuteDashboardQueryAsync("FamilyRelationships list", () => relationshipQuery.ToListAsync());

        var upcomingBirthdays = members
            .Where(member => member.DateOfBirth.HasValue)
            .Select(member => new UpcomingBirthdayViewModel
            {
                Name = member.FirstName + " " + member.LastName,
                ImagePath = member.ImagePath,
                Birthday = member.DateOfBirth!.Value,
                DaysRemaining = CalculateDaysUntilBirthday(member.DateOfBirth!.Value),
                AgeTurning = CalculateAgeTurning(member.DateOfBirth!.Value)
            })
            .Where(item => item.DaysRemaining >= 0 && item.DaysRemaining <= 45)
            .OrderBy(item => item.DaysRemaining)
            .Take(6)
            .ToList();

        var genderChart = BuildChartData(
            members,
            member => string.Equals(member.Gender, "Male", StringComparison.OrdinalIgnoreCase) ? "Male" : "Female");

        var nationalityChart = BuildChartData(
            members,
            member => string.IsNullOrWhiteSpace(member.Nationality) ? "Unknown" : member.Nationality);

        var stateChart = BuildChartData(
            members,
            member => string.IsNullOrWhiteSpace(member.State) ? "Unknown" : member.State);

        var ageChart = BuildChartData(
            members,
            member => GetAgeGroup(member.Age));

        var relationshipChart = BuildChartData(
            relationshipItems,
            relation => string.IsNullOrWhiteSpace(relation.RelationshipType) ? "Other" : relation.RelationshipType);

        var searchResults = members
            .Take(4)
            .Select(member => new DashboardSearchResultViewModel
            {
                Section = "Members",
                Title = member.FullName,
                Subtitle = member.Relationship ?? "Family member",
                Url = Url.Action("Details", "FamilyMembers", new { id = member.Id }),
                ImagePath = member.ImagePath
            })
            .ToList();

        var recentNotifications = string.IsNullOrWhiteSpace(userId)
            ? new List<DashboardNotificationViewModel>()
            : (await ExecuteDashboardQueryAsync("Notifications recent", () => _notificationService.GetRecentForUserAsync(userId, 5)))
                .Select(notification => new DashboardNotificationViewModel
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    Link = notification.Link,
                    Icon = notification.Icon,
                    CreatedAt = notification.CreatedAt,
                    IsRead = notification.IsRead
                })
                .ToList();

        var dashboardViewModel = new DashboardViewModel
        {
            TotalFamilyMembers = members.Count,
            TotalRegisteredUsers = totalRegisteredUsers,
            TotalRelationships = totalRelationships,
            TotalMales = members.Count(member => string.Equals(member.Gender, "Male", StringComparison.OrdinalIgnoreCase)),
            TotalFemales = members.Count(member => string.Equals(member.Gender, "Female", StringComparison.OrdinalIgnoreCase)),
            MarriedMembers = members.Count(member => string.Equals(member.MaritalStatus, "Married", StringComparison.OrdinalIgnoreCase)),
            SingleMembers = members.Count(member => string.Equals(member.MaritalStatus, "Single", StringComparison.OrdinalIgnoreCase)),
            AverageAge = members.Where(member => member.DateOfBirth.HasValue).Select(member => member.Age ?? 0).DefaultIfEmpty(0).Average(),
            YoungestMember = members.Where(member => member.DateOfBirth.HasValue).OrderBy(member => member.DateOfBirth).Select(member => member.FirstName + " " + member.LastName).FirstOrDefault() ?? "No members yet",
            OldestMember = members.Where(member => member.DateOfBirth.HasValue).OrderByDescending(member => member.DateOfBirth).Select(member => member.FirstName + " " + member.LastName).FirstOrDefault() ?? "No members yet",
            UpcomingBirthdays = upcomingBirthdays,
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
                .ToList(),
            RecentActivities = recentActivities,
            RecentNotifications = recentNotifications,
            SearchResults = searchResults,
            GenderChartDataJson = JsonSerializer.Serialize(genderChart),
            NationalityChartDataJson = JsonSerializer.Serialize(nationalityChart),
            StateChartDataJson = JsonSerializer.Serialize(stateChart),
            AgeGroupChartDataJson = JsonSerializer.Serialize(ageChart),
            RelationshipChartDataJson = JsonSerializer.Serialize(relationshipChart)
        };

        return View("Index", dashboardViewModel);
    }

    private async Task<T> ExecuteDashboardQueryAsync<T>(string queryName, Func<Task<T>> query)
    {
        try
        {
            var result = await query();
            _logger.LogInformation("Dashboard query completed: {QueryName}", queryName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard query failed: {QueryName}, UserId: {UserId}, Path: {Path}", queryName, User.FindFirst(ClaimTypes.NameIdentifier)?.Value, Request.Path);
            Console.WriteLine($"[DashboardQuery] Failed; Query={queryName}; UserId={User.FindFirst(ClaimTypes.NameIdentifier)?.Value}; Path={Request.Path}{Environment.NewLine}{ex}");
            throw;
        }
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

    private static int CalculateAgeTurning(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return age + 1;
    }

    private static string GetAgeGroup(int? age)
    {
        if (age is null || age < 0)
        {
            return "Unknown";
        }

        if (age < 13)
        {
            return "Child";
        }

        if (age < 20)
        {
            return "Teen";
        }

        if (age < 40)
        {
            return "Adult";
        }

        if (age < 60)
        {
            return "Middle Age";
        }

        return "Senior";
    }

    private static IReadOnlyList<ChartDataPoint> BuildChartData<T>(IEnumerable<T> items, Func<T, string> selector)
    {
        return items
            .Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value)
            .Select(group => new ChartDataPoint(group.Key, group.Count()))
            .OrderByDescending(point => point.Count)
            .Take(6)
            .ToList();
    }

    private static string ResolveActivityIcon(string action)
    {
        return action.ToLowerInvariant() switch
        {
            "create" => "bi-plus-circle",
            "update" => "bi-pencil-square",
            "delete" => "bi-trash",
            "login" => "bi-box-arrow-in-right",
            "logout" => "bi-box-arrow-right",
            _ => "bi-journal-text"
        };
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

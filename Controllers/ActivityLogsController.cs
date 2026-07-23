using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Controllers;

[Authorize(Roles = "Admin")]
public class ActivityLogsController : Controller
{
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ActivityLogsController> _logger;

    public ActivityLogsController(IActivityLogService activityLogService, INotificationService notificationService, ILogger<ActivityLogsController> logger)
    {
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo, int pageNumber = 1)
    {
        const int pageSize = 20;
        var logs = await _activityLogService.SearchAsync(searchTerm, actionFilter, userFilter, dateFrom, dateTo, pageNumber, pageSize);
        var totalCount = await _activityLogService.GetTotalCountAsync(searchTerm, actionFilter, userFilter, dateFrom, dateTo);
        var stats = await _activityLogService.GetStatisticsAsync();
        var actions = await _activityLogService.GetDistinctActionsAsync();
        var users = await _activityLogService.GetDistinctUsersAsync();

        var model = new ActivityLogIndexViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            ActionFilter = actionFilter ?? string.Empty,
            UserFilter = userFilter ?? string.Empty,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Logs = logs,
            Statistics = stats,
            AvailableActions = actions,
            AvailableUsers = users
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportCsv(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        var csv = await _activityLogService.ExportCsvAsync(searchTerm, actionFilter, userFilter, dateFrom, dateTo);

        try
        {
            await _notificationService.CreateForAdminsAsync(
                "Data export completed",
                "The activity log export was generated successfully.",
                Url.Action(nameof(Index), "ActivityLogs"),
                "System",
                null,
                "Success",
                "bi-download");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification failed after activity log CSV export. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Activity log export notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "activity-log.csv");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportExcel(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        var bytes = await _activityLogService.ExportExcelAsync(searchTerm, actionFilter, userFilter, dateFrom, dateTo);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "activity-log.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportPdf(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        var bytes = await _activityLogService.ExportPdfAsync(searchTerm, actionFilter, userFilter, dateFrom, dateTo);
        return File(bytes, "application/pdf", "activity-log.pdf");
    }
}

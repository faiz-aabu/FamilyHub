using System.Text;
using ClosedXML.Excel;
using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace FamilyHub.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly FamilyHubDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivityLogService(FamilyHubDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string description, string? entityName = null, string? entityId = null, bool success = true, string? details = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userId = user?.Identity?.IsAuthenticated == true ? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var userName = userId is null ? null : await _userManager.FindByIdAsync(userId) is ApplicationUser currentUser ? currentUser.FullName ?? currentUser.Email : null;

            var logEntry = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Description = description,
                EntityName = entityName,
                EntityId = entityId,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                Browser = httpContext?.Request.Headers["User-Agent"].ToString(),
                Success = success,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Best-effort audit logging should never break the main workflow.
        }
    }

    public async Task<IReadOnlyList<ActivityLog>> GetRecentAsync(int count = 50)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(log => log.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ActivityLog>> SearchAsync(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize)
    {
        var query = ApplyFilters(_context.ActivityLogs.AsNoTracking(), searchTerm, actionFilter, userFilter, dateFrom, dateTo);

        return await query
            .OrderByDescending(log => log.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        var query = ApplyFilters(_context.ActivityLogs.AsNoTracking(), searchTerm, actionFilter, userFilter, dateFrom, dateTo);
        return await query.CountAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctActionsAsync()
    {
        return await _context.ActivityLogs.AsNoTracking()
            .Where(log => !string.IsNullOrWhiteSpace(log.Action))
            .Select(log => log.Action)
            .Distinct()
            .OrderBy(action => action)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetDistinctUsersAsync()
    {
        return await _context.ActivityLogs.AsNoTracking()
            .Where(log => !string.IsNullOrWhiteSpace(log.UserName))
            .Select(log => log.UserName!)
            .Distinct()
            .OrderBy(userName => userName)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        var logs = await _context.ActivityLogs.AsNoTracking().ToListAsync();
        return new Dictionary<string, int>
        {
            ["Total"] = logs.Count,
            ["Success"] = logs.Count(log => log.Success),
            ["Failed"] = logs.Count(log => !log.Success),
            ["Today"] = logs.Count(log => log.Timestamp.Date == DateTime.UtcNow.Date)
        };
    }

    public async Task<string> ExportCsvAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var logs = await ApplyFilters(_context.ActivityLogs.AsNoTracking(), searchTerm, actionFilter, userFilter, dateFrom, dateTo)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,User,Action,Description,Entity,IP Address,Result");
        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{Escape(log.UserName)},{Escape(log.Action)},{Escape(log.Description)},{Escape(log.EntityName)},{Escape(log.IpAddress)},{(log.Success ? "Success" : "Failure")}");
        }

        return sb.ToString();
    }

    public async Task<byte[]> ExportExcelAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var logs = await ApplyFilters(_context.ActivityLogs.AsNoTracking(), searchTerm, actionFilter, userFilter, dateFrom, dateTo)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Logs");
        worksheet.Cell(1, 1).Value = "Date";
        worksheet.Cell(1, 2).Value = "Time";
        worksheet.Cell(1, 3).Value = "User";
        worksheet.Cell(1, 4).Value = "Action";
        worksheet.Cell(1, 5).Value = "Description";
        worksheet.Cell(1, 6).Value = "Entity";
        worksheet.Cell(1, 7).Value = "IP Address";
        worksheet.Cell(1, 8).Value = "Result";

        for (var index = 0; index < logs.Count; index++)
        {
            var log = logs[index];
            var rowIndex = index + 2;
            worksheet.Cell(rowIndex, 1).Value = log.Timestamp.ToLocalTime().ToString("yyyy-MM-dd");
            worksheet.Cell(rowIndex, 2).Value = log.Timestamp.ToLocalTime().ToString("HH:mm:ss");
            worksheet.Cell(rowIndex, 3).Value = string.IsNullOrWhiteSpace(log.UserName) ? "System" : log.UserName;
            worksheet.Cell(rowIndex, 4).Value = log.Action;
            worksheet.Cell(rowIndex, 5).Value = log.Description;
            worksheet.Cell(rowIndex, 6).Value = log.EntityName ?? string.Empty;
            worksheet.Cell(rowIndex, 7).Value = log.IpAddress ?? string.Empty;
            worksheet.Cell(rowIndex, 8).Value = log.Success ? "Success" : "Failure";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPdfAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var logs = await ApplyFilters(_context.ActivityLogs.AsNoTracking(), searchTerm, actionFilter, userFilter, dateFrom, dateTo)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        var document = new PdfDocument();
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Verdana", 16, XFontStyle.Bold);
        var bodyFont = new XFont("Verdana", 10, XFontStyle.Regular);
        var headerFont = new XFont("Verdana", 10, XFontStyle.Bold);

        graphics.DrawString("FamilyHub Audit Logs", titleFont, XBrushes.Black, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopLeft);
        graphics.DrawString($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}", bodyFont, XBrushes.Black, new XRect(40, 70, page.Width - 80, 20), XStringFormats.TopLeft);

        var y = 100;
        foreach (var log in logs.Take(80))
        {
            var line = $"{log.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm} | {(string.IsNullOrWhiteSpace(log.UserName) ? "System" : log.UserName)} | {log.Action} | {log.Description}";
            graphics.DrawString(line, bodyFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
            y += 15;
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static IQueryable<ActivityLog> ApplyFilters(IQueryable<ActivityLog> query, string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim();
            query = query.Where(log =>
                (log.Description != null && log.Description.Contains(normalized)) ||
                (log.UserName != null && log.UserName.Contains(normalized)) ||
                (log.EntityName != null && log.EntityName.Contains(normalized)) ||
                (log.Action != null && log.Action.Contains(normalized)));
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(log => log.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            var normalizedUser = userFilter.Trim();
            query = query.Where(log => log.UserName != null && log.UserName.Contains(normalizedUser));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(log => log.Timestamp >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(log => log.Timestamp < dateTo.Value.Date.AddDays(1));
        }

        return query;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace('"', '"').Replace("\n", " ");
}

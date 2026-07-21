using ClosedXML.Excel;
using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace FamilyHub.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly FamilyHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;

    public ReportsController(FamilyHubDbContext context, UserManager<ApplicationUser> userManager, IActivityLogService activityLogService)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = await BuildReportViewModelAsync();
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Print()
    {
        var viewModel = await BuildReportViewModelAsync();
        return View("Print", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf()
    {
        var viewModel = await BuildReportViewModelAsync();
        await _activityLogService.LogAsync("Reports Exported", "Exported the reports dashboard as a PDF file.", "Reports", null, true, "PDF report generated.");
        var bytes = GeneratePdf(viewModel);
        return File(bytes, "application/pdf", "familyhub-report.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var viewModel = await BuildReportViewModelAsync();
        await _activityLogService.LogAsync("Reports Exported", "Exported the reports dashboard as an Excel workbook.", "Reports", null, true, "Excel report generated.");
        var bytes = GenerateExcel(viewModel);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "familyhub-report.xlsx");
    }

    private async Task<ReportsViewModel> BuildReportViewModelAsync()
    {
        var members = await _context.FamilyMembers.AsNoTracking().ToListAsync();
        var relationships = await _context.FamilyRelationships.AsNoTracking().ToListAsync();
        var logs = await _context.ActivityLogs.AsNoTracking().OrderByDescending(log => log.Timestamp).Take(8).ToListAsync();
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var administrators = users.Count(user => _userManager.IsInRoleAsync(user, "Admin").Result);

        return new ReportsViewModel
        {
            TotalMembers = members.Count,
            MaleMembers = members.Count(member => string.Equals(member.Gender, "Male", StringComparison.OrdinalIgnoreCase)),
            FemaleMembers = members.Count(member => string.Equals(member.Gender, "Female", StringComparison.OrdinalIgnoreCase)),
            Children = members.Count(member => member.Age is >= 0 and < 18),
            Adults = members.Count(member => member.Age is >= 18 and < 60),
            Seniors = members.Count(member => member.Age is >= 60),
            TotalRelationships = relationships.Count,
            Families = members.Select(member => member.RelatedFamilyMemberId).Distinct().Count(),
            Users = users.Count,
            Administrators = administrators,
            TotalActivities = logs.Count,
            RecentLogs = logs,
            MembersByGender = members
                .Where(member => !string.IsNullOrWhiteSpace(member.Gender))
                .GroupBy(member => member.Gender!)
                .OrderByDescending(group => group.Count())
                .Select(group => new ReportMetric { Label = group.Key, Value = group.Count() })
                .ToList(),
            AgeDistribution = members
                .Where(member => member.Age.HasValue)
                .GroupBy(member => member.Age!.Value switch
                {
                    < 18 => "Children",
                    < 60 => "Adults",
                    _ => "Seniors"
                })
                .Select(group => new ReportMetric { Label = group.Key, Value = group.Count() })
                .OrderByDescending(item => item.Value)
                .ToList(),
            MonthlyRegistrations = users
                .GroupBy(user => user.CreatedAt?.DateTime.ToString("MMM yyyy") ?? "Unknown")
                .Select(group => new ReportMetric { Label = group.Key, Value = group.Count() })
                .OrderBy(item => item.Label)
                .ToList(),
            RelationshipsByType = relationships
                .Where(relationship => !string.IsNullOrWhiteSpace(relationship.RelationshipType))
                .GroupBy(relationship => relationship.RelationshipType)
                .OrderByDescending(group => group.Count())
                .Select(group => new ReportMetric { Label = group.Key, Value = group.Count() })
                .ToList(),
            UserActivity = (await _activityLogService.GetRecentAsync(20))
                .Where(log => !string.IsNullOrWhiteSpace(log.UserName))
                .GroupBy(log => log.UserName!)
                .Select(group => new ReportMetric { Label = group.Key, Value = group.Count() })
                .OrderByDescending(item => item.Value)
                .Take(6)
                .ToList()
        };
    }

    private static byte[] GeneratePdf(ReportsViewModel viewModel)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var graphics = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Verdana", 18, XFontStyle.Bold);
        var bodyFont = new XFont("Verdana", 11, XFontStyle.Regular);

        graphics.DrawString("FamilyHub Report", titleFont, XBrushes.Black, new XRect(40, 40, 500, 30), XStringFormats.TopLeft);
        graphics.DrawString($"Members: {viewModel.TotalMembers} | Relationships: {viewModel.TotalRelationships} | Users: {viewModel.Users}", bodyFont, XBrushes.Black, new XRect(40, 80, 500, 20), XStringFormats.TopLeft);
        graphics.DrawString($"Male: {viewModel.MaleMembers} | Female: {viewModel.FemaleMembers} | Children: {viewModel.Children} | Adults: {viewModel.Adults} | Seniors: {viewModel.Seniors}", bodyFont, XBrushes.Black, new XRect(40, 105, 500, 20), XStringFormats.TopLeft);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static byte[] GenerateExcel(ReportsViewModel viewModel)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");
        worksheet.Cell(1, 1).Value = "Metric";
        worksheet.Cell(1, 2).Value = "Value";
        worksheet.Cell(2, 1).Value = "Total Members";
        worksheet.Cell(2, 2).Value = viewModel.TotalMembers;
        worksheet.Cell(3, 1).Value = "Male Members";
        worksheet.Cell(3, 2).Value = viewModel.MaleMembers;
        worksheet.Cell(4, 1).Value = "Female Members";
        worksheet.Cell(4, 2).Value = viewModel.FemaleMembers;
        worksheet.Cell(5, 1).Value = "Children";
        worksheet.Cell(5, 2).Value = viewModel.Children;
        worksheet.Cell(6, 1).Value = "Adults";
        worksheet.Cell(6, 2).Value = viewModel.Adults;
        worksheet.Cell(7, 1).Value = "Seniors";
        worksheet.Cell(7, 2).Value = viewModel.Seniors;
        worksheet.Cell(8, 1).Value = "Relationships";
        worksheet.Cell(8, 2).Value = viewModel.TotalRelationships;
        worksheet.Cell(9, 1).Value = "Users";
        worksheet.Cell(9, 2).Value = viewModel.Users;
        worksheet.Cell(10, 1).Value = "Administrators";
        worksheet.Cell(10, 2).Value = viewModel.Administrators;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}


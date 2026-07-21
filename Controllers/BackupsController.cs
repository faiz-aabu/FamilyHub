using FamilyHub.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Controllers;

[Authorize(Roles = "Admin")]
public class BackupsController : Controller
{
    private readonly IBackupService _backupService;
    private readonly INotificationService _notificationService;

    public BackupsController(IBackupService backupService, INotificationService notificationService)
    {
        _backupService = backupService;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? sortOrder)
    {
        var model = await _backupService.GetBackupIndexAsync(searchTerm, sortOrder);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? backupName)
    {
        try
        {
            var fileName = await _backupService.CreateBackupAsync(backupName);
            TempData["SuccessMessage"] = $"Backup created successfully: {fileName}";

            try
            {
                await _notificationService.CreateForAdminsAsync(
                    "Backup completed",
                    $"A database backup named {fileName} was created successfully.",
                    Url.Action(nameof(Index), "Backups"),
                    "System",
                    null,
                    "Success",
                    "bi-cloud-arrow-up-fill");
            }
            catch
            {
                // Notification delivery errors should not block backup creation.
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string fileName)
    {
        try
        {
            var success = await _backupService.RestoreBackupAsync(fileName);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Backup restored successfully." : "The selected backup could not be restored.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string fileName)
    {
        try
        {
            var success = await _backupService.DeleteBackupAsync(fileName);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Backup deleted successfully." : "The selected backup could not be deleted.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(string oldFileName, string newFileName)
    {
        try
        {
            var success = await _backupService.RenameBackupAsync(oldFileName, newFileName);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Backup renamed successfully." : "The selected backup could not be renamed.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(string fileName)
    {
        var bytes = await _backupService.DownloadBackupAsync(fileName);
        if (bytes is null)
        {
            return NotFound();
        }

        return File(bytes, "application/octet-stream", fileName);
    }

    public async Task<IActionResult> Details(string fileName)
    {
        var model = await _backupService.GetBackupDetailsAsync(fileName);
        return model is null ? NotFound() : View(model);
    }
}

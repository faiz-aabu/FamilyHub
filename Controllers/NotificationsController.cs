using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? type, string? readFilter, int pageNumber = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        var viewModel = await _notificationService.GetPagedForUserAsync(user.Id, searchTerm, type, readFilter, pageNumber, 10);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        await _notificationService.MarkAsReadAsync(id, user.Id);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUnread(int id, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        await _notificationService.SetReadStateAsync(id, user.Id, false);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        await _notificationService.MarkAllAsReadAsync(user.Id);
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        await _notificationService.DeleteAsync(id, user.Id);
        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { count = 0 });
        }

        var count = await _notificationService.GetUnreadCountAsync(user.Id);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(Array.Empty<object>());
        }

        var notifications = await _notificationService.GetRecentForUserAsync(user.Id, 8);
        return Json(notifications.Select(item => new
        {
            id = item.Id,
            title = item.Title,
            message = item.Message,
            type = item.Type,
            icon = item.Icon,
            link = item.Link,
            createdAt = item.CreatedAt,
            isRead = item.IsRead,
            timeAgo = item.GetTimeAgo()
        }));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}

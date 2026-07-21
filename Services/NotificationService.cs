using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Services;

public class NotificationService : INotificationService
{
    private readonly FamilyHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<FamilyHub.Hubs.NotificationHub> _hubContext;

    public NotificationService(FamilyHubDbContext context, UserManager<ApplicationUser> userManager, IHubContext<FamilyHub.Hubs.NotificationHub> hubContext)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task CreateAsync(string userId, string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null)
    {
        await CreateNotificationsAsync(new[] { userId }, title, message, linkUrl, relatedEntityType, relatedEntityId, type, icon);
    }

    public async Task CreateForUserAndAdminsAsync(string userId, string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null)
    {
        var recipientIds = new List<string> { userId };
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        recipientIds.AddRange(adminUsers.Select(user => user.Id));

        await CreateNotificationsAsync(recipientIds.Distinct(StringComparer.OrdinalIgnoreCase), title, message, linkUrl, relatedEntityType, relatedEntityId, type, icon);
    }

    public async Task CreateForAdminsAsync(string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null)
    {
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var recipientIds = adminUsers.Select(user => user.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (recipientIds.Count == 0)
        {
            return;
        }

        await CreateNotificationsAsync(recipientIds, title, message, linkUrl, relatedEntityType, relatedEntityId, type, icon);
    }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(string userId, int take = 50)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Notification>> GetRecentForUserAsync(string userId, int take = 5)
    {
        return await GetForUserAsync(userId, take);
    }

    public async Task<NotificationListViewModel> GetPagedForUserAsync(string userId, string? searchTerm, string? type, string? readFilter, int pageNumber, int pageSize)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalized = searchTerm.Trim();
            query = query.Where(notification => notification.Title.Contains(normalized) || notification.Message.Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(notification => notification.Type == type);
        }

        if (string.Equals(readFilter, "read", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(notification => notification.IsRead);
        }
        else if (string.Equals(readFilter, "unread", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(notification => !notification.IsRead);
        }

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize)));
        var currentPage = Math.Clamp(pageNumber, 1, totalPages);

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var availableTypes = await _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .Select(notification => notification.Type)
            .Distinct()
            .OrderBy(typeName => typeName)
            .ToListAsync();

        return new NotificationListViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            SelectedType = type,
            SelectedReadFilter = readFilter ?? "all",
            PageNumber = currentPage,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Notifications = notifications,
            AvailableTypes = availableTypes
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications.CountAsync(notification => notification.UserId == userId && !notification.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        await SetReadStateAsync(notificationId, userId, true);
    }

    public async Task SetReadStateAsync(int notificationId, string userId, bool isRead)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId);
        if (notification is null)
        {
            return;
        }

        notification.IsRead = isRead;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications.Where(item => item.UserId == userId && !item.IsRead).ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId);
        if (notification is null)
        {
            return;
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
    }

    private async Task CreateNotificationsAsync(IEnumerable<string> recipientIds, string title, string message, string? linkUrl, string? relatedEntityType, int? relatedEntityId, string type, string? icon)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "Information" : type;
        var normalizedIcon = string.IsNullOrWhiteSpace(icon) ? ResolveDefaultIcon(normalizedType) : icon;
        var recipientIdList = recipientIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var allowedRecipients = await _userManager.Users
            .AsNoTracking()
            .Where(user => recipientIdList.Contains(user.Id))
            .Where(user => user.NotificationPreference)
            .Select(user => user.Id)
            .ToListAsync();

        var notifications = new List<Notification>();
        foreach (var recipientId in allowedRecipients)
        {
            notifications.Add(new Notification
            {
                UserId = recipientId,
                Title = title,
                Message = message,
                Link = linkUrl,
                Type = normalizedType,
                Icon = normalizedIcon,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (notifications.Count == 0)
        {
            return;
        }

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        var payload = notifications.Select(notification => new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.Link,
            notification.Icon,
            notification.CreatedAt,
            notification.IsRead
        });

        await _hubContext.Clients.Users(notifications.Select(notification => notification.UserId)).SendAsync("ReceiveNotification", payload);
    }

    private static string ResolveDefaultIcon(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "success" => "bi-check-circle-fill",
            "warning" => "bi-exclamation-triangle-fill",
            "error" => "bi-x-circle-fill",
            _ => "bi-info-circle-fill"
        };
    }
}

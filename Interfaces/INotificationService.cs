using FamilyHub.Models;

namespace FamilyHub.Interfaces;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null);
    Task CreateForUserAndAdminsAsync(string userId, string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null);
    Task CreateForAdminsAsync(string title, string message, string? linkUrl = null, string? relatedEntityType = null, int? relatedEntityId = null, string type = "Information", string? icon = null);
    Task<IReadOnlyList<Notification>> GetForUserAsync(string userId, int take = 50);
    Task<IReadOnlyList<Notification>> GetRecentForUserAsync(string userId, int take = 5);
    Task<NotificationListViewModel> GetPagedForUserAsync(string userId, string? searchTerm, string? type, string? readFilter, int pageNumber, int pageSize);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task SetReadStateAsync(int notificationId, string userId, bool isRead);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteAsync(int notificationId, string userId);
}

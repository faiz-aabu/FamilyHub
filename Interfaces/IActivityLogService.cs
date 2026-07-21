using FamilyHub.Models;

namespace FamilyHub.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(string action, string description, string? entityName = null, string? entityId = null, bool success = true, string? details = null);
    Task<IReadOnlyList<ActivityLog>> GetRecentAsync(int count = 50);
    Task<IReadOnlyList<ActivityLog>> SearchAsync(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo, int pageNumber, int pageSize);
    Task<int> GetTotalCountAsync(string? searchTerm, string? actionFilter, string? userFilter, DateTime? dateFrom, DateTime? dateTo);
    Task<IReadOnlyList<string>> GetDistinctActionsAsync();
    Task<IReadOnlyList<string>> GetDistinctUsersAsync();
    Task<Dictionary<string, int>> GetStatisticsAsync();
    Task<string> ExportCsvAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<byte[]> ExportExcelAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    Task<byte[]> ExportPdfAsync(string? searchTerm = null, string? actionFilter = null, string? userFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null);
}

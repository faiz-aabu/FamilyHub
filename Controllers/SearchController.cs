using System.Security.Claims;
using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly FamilyHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;

    public SearchController(FamilyHubDbContext context, UserManager<ApplicationUser> userManager, IActivityLogService activityLogService, INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query, string? sortBy, string? gender, int? minAge, int? maxAge, string? nationality, string? state, string? religion, string? occupation, string? maritalStatus, string? relationshipType, string? familyBranch, DateTime? createdFrom, DateTime? createdTo, DateTime? updatedFrom, DateTime? updatedTo, int pageNumber = 1, bool showFilters = false)
    {
        var searchTerm = query ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            await _activityLogService.LogAsync("Search Performed", $"Searched for '{searchTerm}'.", "Search", null, true, "User search executed.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewModel = new SearchViewModel
        {
            Query = searchTerm,
            SortBy = sortBy ?? "relevance",
            Gender = gender,
            MinAge = minAge,
            MaxAge = maxAge,
            Nationality = nationality,
            State = state,
            Religion = religion,
            Occupation = occupation,
            MaritalStatus = maritalStatus,
            RelationshipType = relationshipType,
            FamilyBranch = familyBranch,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            UpdatedFrom = updatedFrom,
            UpdatedTo = updatedTo,
            PageNumber = Math.Max(1, pageNumber),
            ShowFilters = showFilters,
            HasQuery = !string.IsNullOrWhiteSpace(searchTerm),
            HasResults = false,
            RecentSearches = GetRecentSearches(),
            Suggestions = GetSuggestions(searchTerm),
            AvailableGenders = await _context.FamilyMembers.AsNoTracking().Where(member => !string.IsNullOrWhiteSpace(member.Gender)).Select(member => member.Gender!).Distinct().OrderBy(value => value).ToListAsync(),
            AvailableNationalities = await _context.FamilyMembers.AsNoTracking().Where(member => !string.IsNullOrWhiteSpace(member.Nationality)).Select(member => member.Nationality!).Distinct().OrderBy(value => value).ToListAsync(),
            AvailableStates = await _context.FamilyMembers.AsNoTracking().Where(member => !string.IsNullOrWhiteSpace(member.State)).Select(member => member.State!).Distinct().OrderBy(value => value).ToListAsync(),
            AvailableOccupations = await _context.FamilyMembers.AsNoTracking().Where(member => !string.IsNullOrWhiteSpace(member.Occupation)).Select(member => member.Occupation!).Distinct().OrderBy(value => value).ToListAsync(),
            AvailableMaritalStatuses = await _context.FamilyMembers.AsNoTracking().Where(member => !string.IsNullOrWhiteSpace(member.MaritalStatus)).Select(member => member.MaritalStatus!).Distinct().OrderBy(value => value).ToListAsync(),
            AvailableRelationshipTypes = await _context.FamilyRelationships.AsNoTracking().Where(relation => !string.IsNullOrWhiteSpace(relation.RelationshipType)).Select(relation => relation.RelationshipType!).Distinct().OrderBy(value => value).ToListAsync()
        };

        try
        {
            var familyMembers = await SearchMembersAsync(searchTerm, gender, minAge, maxAge, nationality, state, religion, occupation, maritalStatus, relationshipType, familyBranch, createdFrom, createdTo, updatedFrom, updatedTo, sortBy);
            var relationships = await SearchRelationshipsAsync(searchTerm, relationshipType, familyBranch, sortBy);
            var users = await SearchUsersAsync(searchTerm, sortBy);
            var notifications = await SearchNotificationsAsync(currentUserId, searchTerm, sortBy);
            var auditLogs = await SearchAuditLogsAsync(searchTerm, sortBy);
            var reports = await SearchReportsAsync(searchTerm, sortBy);

            viewModel.FamilyMembers = familyMembers;
            viewModel.Relationships = relationships;
            viewModel.Users = users;
            viewModel.Notifications = notifications;
            viewModel.AuditLogs = auditLogs;
            viewModel.Reports = reports;
            viewModel.HasResults = familyMembers.Any() || relationships.Any() || users.Any() || notifications.Any() || auditLogs.Any() || reports.Any();
        }
        catch (Exception)
        {
            viewModel.ErrorMessage = "Search could not be completed right now. Please try again or contact support if the issue continues.";
            viewModel.HasResults = false;
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string? query)
    {
        return Json(GetSuggestions(query));
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchMembersAsync(string query, string? gender, int? minAge, int? maxAge, string? nationality, string? state, string? religion, string? occupation, string? maritalStatus, string? relationshipType, string? familyBranch, DateTime? createdFrom, DateTime? createdTo, DateTime? updatedFrom, DateTime? updatedTo, string? sortBy)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var membersQuery = _context.FamilyMembers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            membersQuery = membersQuery.Where(member =>
                member.Id.ToString().Contains(normalizedQuery) ||
                (member.FirstName != null && member.FirstName.Contains(normalizedQuery)) ||
                (member.LastName != null && member.LastName.Contains(normalizedQuery)) ||
                (member.FullName != null && member.FullName.Contains(normalizedQuery)) ||
                (member.Nickname != null && member.Nickname.Contains(normalizedQuery)) ||
                (member.Email != null && member.Email.Contains(normalizedQuery)) ||
                (member.PhoneNumber != null && member.PhoneNumber.Contains(normalizedQuery)) ||
                (member.Occupation != null && member.Occupation.Contains(normalizedQuery)) ||
                (member.Address != null && member.Address.Contains(normalizedQuery)) ||
                (member.Nationality != null && member.Nationality.Contains(normalizedQuery)) ||
                (member.State != null && member.State.Contains(normalizedQuery)) ||
                (member.Religion != null && member.Religion.Contains(normalizedQuery)) ||
                (member.BloodGroup != null && member.BloodGroup.Contains(normalizedQuery)) ||
                (member.Gender != null && member.Gender.Contains(normalizedQuery)) ||
                (member.Relationship != null && member.Relationship.Contains(normalizedQuery)) ||
                (member.Hobbies != null && member.Hobbies.Contains(normalizedQuery)) ||
                (member.Notes != null && member.Notes.Contains(normalizedQuery)) ||
                (member.FirstName + " " + member.LastName).Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            membersQuery = membersQuery.Where(member => member.Gender == gender);
        }

        if (minAge.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.DateOfBirth.HasValue && member.Age >= minAge.Value);
        }

        if (maxAge.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.DateOfBirth.HasValue && member.Age <= maxAge.Value);
        }

        if (!string.IsNullOrWhiteSpace(nationality))
        {
            membersQuery = membersQuery.Where(member => member.Nationality == nationality);
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            membersQuery = membersQuery.Where(member => member.State == state);
        }

        if (!string.IsNullOrWhiteSpace(religion))
        {
            membersQuery = membersQuery.Where(member => member.Religion == religion);
        }

        if (!string.IsNullOrWhiteSpace(occupation))
        {
            membersQuery = membersQuery.Where(member => member.Occupation == occupation);
        }

        if (!string.IsNullOrWhiteSpace(maritalStatus))
        {
            membersQuery = membersQuery.Where(member => member.MaritalStatus == maritalStatus);
        }

        if (!string.IsNullOrWhiteSpace(relationshipType))
        {
            membersQuery = membersQuery.Where(member => member.Relationship == relationshipType);
        }

        if (!string.IsNullOrWhiteSpace(familyBranch))
        {
            membersQuery = membersQuery.Where(member =>
                (member.Relationship != null && member.Relationship == familyBranch) ||
                (member.Notes != null && member.Notes.Contains(familyBranch)));
        }

        if (createdFrom.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.CreatedAt <= createdTo.Value);
        }

        if (updatedFrom.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.UpdatedAt >= updatedFrom.Value);
        }

        if (updatedTo.HasValue)
        {
            membersQuery = membersQuery.Where(member => member.UpdatedAt <= updatedTo.Value);
        }

        membersQuery = sortBy switch
        {
            "name-desc" => membersQuery.OrderByDescending(member => member.FirstName).ThenByDescending(member => member.LastName),
            "newest" => membersQuery.OrderByDescending(member => member.CreatedAt),
            "oldest" => membersQuery.OrderBy(member => member.CreatedAt),
            "age" => membersQuery.OrderByDescending(member => member.DateOfBirth),
            "relationships" => membersQuery.OrderByDescending(member => member.Relationships.Count),
            _ => membersQuery.OrderBy(member => member.FirstName).ThenBy(member => member.LastName)
        };

        var members = await membersQuery.Take(6).ToListAsync();
        return members.Select(member => new SearchResultItemViewModel
        {
            Category = "Family Members",
            Title = member.FullName,
            Subtitle = member.Relationship ?? "Family Member",
            Description = member.Email ?? member.PhoneNumber ?? member.Address ?? string.Empty,
            Url = Url.Action("Details", "FamilyMembers", new { id = member.Id }),
            Icon = "bi-people-fill",
            Badge = member.Gender ?? "Member",
            CreatedAt = member.CreatedAt,
            UpdatedAt = member.UpdatedAt,
            Age = member.Age,
            HighlightedTitle = SearchResultItemViewModel.Highlight(member.FullName, normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight(member.Relationship, normalizedQuery),
            HighlightedDescription = SearchResultItemViewModel.Highlight(member.Email ?? member.PhoneNumber ?? member.Address ?? string.Empty, normalizedQuery)
        }).ToList();
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchRelationshipsAsync(string query, string? relationshipType, string? familyBranch, string? sortBy)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var relationshipQuery = _context.FamilyRelationships.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            relationshipQuery = relationshipQuery.Where(relation =>
                (relation.RelationshipType != null && relation.RelationshipType.Contains(normalizedQuery)) ||
                relation.MemberId.ToString().Contains(normalizedQuery) ||
                relation.RelatedMemberId.ToString().Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(relationshipType))
        {
            relationshipQuery = relationshipQuery.Where(relation => relation.RelationshipType == relationshipType);
        }

        if (!string.IsNullOrWhiteSpace(familyBranch))
        {
            relationshipQuery = relationshipQuery.Where(relation => relation.RelationshipType == familyBranch);
        }

        relationshipQuery = sortBy switch
        {
            "newest" => relationshipQuery.OrderByDescending(relation => relation.Id),
            "oldest" => relationshipQuery.OrderBy(relation => relation.Id),
            _ => relationshipQuery.OrderBy(relation => relation.RelationshipType)
        };

        var relationships = await relationshipQuery.Take(6).ToListAsync();
        return relationships.Select(relation => new SearchResultItemViewModel
        {
            Category = "Relationships",
            Title = relation.RelationshipType ?? "Relationship",
            Subtitle = $"Member {relation.MemberId} → Related member {relation.RelatedMemberId}",
            Description = "Family relationship record",
            Url = Url.Action("Index", "FamilyRelationships"),
            Icon = "bi-diagram-3-fill",
            Badge = relation.RelationshipType ?? "Relationship",
            HighlightedTitle = SearchResultItemViewModel.Highlight(relation.RelationshipType, normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight($"Member {relation.MemberId} → Related member {relation.RelatedMemberId}", normalizedQuery)
        }).ToList();
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchUsersAsync(string query, string? sortBy)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            usersQuery = usersQuery.Where(user =>
                (user.FullName != null && user.FullName.Contains(normalizedQuery)) ||
                (user.Email != null && user.Email.Contains(normalizedQuery)) ||
                (user.UserName != null && user.UserName.Contains(normalizedQuery)));
        }

        usersQuery = sortBy switch
        {
            "name-desc" => usersQuery.OrderByDescending(user => user.FullName),
            "newest" => usersQuery.OrderByDescending(user => user.CreatedAt),
            "oldest" => usersQuery.OrderBy(user => user.CreatedAt),
            _ => usersQuery.OrderBy(user => user.FullName)
        };

        var users = await usersQuery.Take(6).ToListAsync();
        return users.Select(user => new SearchResultItemViewModel
        {
            Category = "Users",
            Title = user.FullName ?? user.Email ?? user.UserName ?? "User",
            Subtitle = user.Email ?? "User account",
            Description = user.PhoneNumber ?? string.Empty,
            Url = Url.Action("Index", "Users"),
            Icon = "bi-person-badge-fill",
            Badge = "Account",
            CreatedAt = user.CreatedAt?.UtcDateTime,
            HighlightedTitle = SearchResultItemViewModel.Highlight(user.FullName ?? user.Email ?? user.UserName ?? "User", normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight(user.Email, normalizedQuery)
        }).ToList();
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchNotificationsAsync(string? userId, string query, string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<SearchResultItemViewModel>();
        }

        var normalizedQuery = (query ?? string.Empty).Trim();
        var notificationsQuery = _context.Notifications.AsNoTracking().Where(notification => notification.UserId == userId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            notificationsQuery = notificationsQuery.Where(notification => (notification.Title ?? string.Empty).Contains(normalizedQuery) || (notification.Message ?? string.Empty).Contains(normalizedQuery));
        }

        notificationsQuery = sortBy switch
        {
            "newest" => notificationsQuery.OrderByDescending(notification => notification.CreatedAt),
            "oldest" => notificationsQuery.OrderBy(notification => notification.CreatedAt),
            _ => notificationsQuery.OrderByDescending(notification => notification.CreatedAt)
        };

        var notifications = await notificationsQuery.Take(6).ToListAsync();
        return notifications.Select(notification => new SearchResultItemViewModel
        {
            Category = "Notifications",
            Title = notification.Title ?? "Notification",
            Subtitle = notification.Type ?? "Information",
            Description = notification.Message ?? string.Empty,
            Url = notification.Link ?? Url.Action("Index", "Notifications"),
            Icon = "bi-bell-fill",
            Badge = notification.IsRead ? "Read" : "Unread",
            CreatedAt = notification.CreatedAt,
            HighlightedTitle = SearchResultItemViewModel.Highlight(notification.Title, normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight(notification.Type, normalizedQuery),
            HighlightedDescription = SearchResultItemViewModel.Highlight(notification.Message, normalizedQuery)
        }).ToList();
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchAuditLogsAsync(string query, string? sortBy)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var logsQuery = _context.ActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            logsQuery = logsQuery.Where(log =>
                (log.Action ?? string.Empty).Contains(normalizedQuery) ||
                (log.Description ?? string.Empty).Contains(normalizedQuery) ||
                (log.UserName != null && log.UserName.Contains(normalizedQuery)) ||
                (log.EntityName != null && log.EntityName.Contains(normalizedQuery)));
        }

        logsQuery = sortBy switch
        {
            "newest" => logsQuery.OrderByDescending(log => log.Timestamp),
            "oldest" => logsQuery.OrderBy(log => log.Timestamp),
            _ => logsQuery.OrderByDescending(log => log.Timestamp)
        };

        var logs = await logsQuery.Take(6).ToListAsync();
        return logs.Select(log => new SearchResultItemViewModel
        {
            Category = "Audit Logs",
            Title = log.Action ?? "Audit action",
            Subtitle = log.UserName ?? "System",
            Description = log.Description ?? string.Empty,
            Url = Url.Action("Index", "ActivityLogs"),
            Icon = "bi-journal-text",
            Badge = log.Success ? "Success" : "Failure",
            CreatedAt = log.Timestamp,
            HighlightedTitle = SearchResultItemViewModel.Highlight(log.Action, normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight(log.UserName, normalizedQuery),
            HighlightedDescription = SearchResultItemViewModel.Highlight(log.Description, normalizedQuery)
        }).ToList();
    }

    private async Task<IReadOnlyList<SearchResultItemViewModel>> SearchReportsAsync(string query, string? sortBy)
    {
        var normalizedQuery = (query ?? string.Empty).Trim();
        var reportsQuery = _context.FamilyMembers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            reportsQuery = reportsQuery.Where(member =>
                (member.FirstName != null && member.FirstName.Contains(normalizedQuery)) ||
                (member.LastName != null && member.LastName.Contains(normalizedQuery)) ||
                (member.Email != null && member.Email.Contains(normalizedQuery)) ||
                (member.Nationality != null && member.Nationality.Contains(normalizedQuery)) ||
                (member.State != null && member.State.Contains(normalizedQuery)) ||
                (member.Relationship != null && member.Relationship.Contains(normalizedQuery)));
        }

        reportsQuery = sortBy switch
        {
            "newest" => reportsQuery.OrderByDescending(member => member.CreatedAt),
            "oldest" => reportsQuery.OrderBy(member => member.CreatedAt),
            _ => reportsQuery.OrderBy(member => member.FirstName)
        };

        var members = await reportsQuery.Take(6).ToListAsync();
        return members.Select(member => new SearchResultItemViewModel
        {
            Category = "Reports",
            Title = $"Report for {member.FullName}",
            Subtitle = member.Relationship ?? "Family profile",
            Description = member.Email ?? member.PhoneNumber ?? string.Empty,
            Url = Url.Action("Index", "Reports"),
            Icon = "bi-bar-chart-line",
            Badge = "Insight",
            CreatedAt = member.CreatedAt,
            HighlightedTitle = SearchResultItemViewModel.Highlight($"Report for {member.FullName}", normalizedQuery),
            HighlightedSubtitle = SearchResultItemViewModel.Highlight(member.Relationship, normalizedQuery)
        }).ToList();
    }

    private static IReadOnlyList<string> GetRecentSearches()
    {
        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> GetSuggestions(string? query)
    {
        var baseSuggestions = new[]
        {
            "Adele Johnson",
            "Sarah Ahmed",
            "Admin user",
            "Relationship parent",
            "Recent activity"
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            return baseSuggestions;
        }

        return baseSuggestions.Where(item => item.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

using System.Security.Claims;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FamilyHub.Controllers;

[Authorize]
public class FamilyRelationshipsController : Controller
{
    private readonly IFamilyRelationshipService _relationshipService;
    private readonly IFamilyMemberService _memberService;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<FamilyRelationshipsController> _logger;

    public FamilyRelationshipsController(IFamilyRelationshipService relationshipService, IFamilyMemberService memberService, IActivityLogService activityLogService, INotificationService notificationService, ILogger<FamilyRelationshipsController> logger)
    {
        _relationshipService = relationshipService;
        _memberService = memberService;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public IActionResult Index(string? searchTerm)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        var relationships = _relationshipService.Search(searchTerm, currentUserId, isAdmin);
        ViewBag.SearchTerm = searchTerm;
        return View(relationships);
    }

    public async Task<IActionResult> Create()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        await PopulateViewBagAsync(currentUserId, isAdmin);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FamilyRelationship relationship)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        await PopulateViewBagAsync(currentUserId, isAdmin);

        if (!ModelState.IsValid)
        {
            try
            {
                foreach (var entry in ModelState)
                {
                    if (entry.Value?.Errors?.Count > 0)
                    {
                        var key = entry.Key ?? "(empty)";
                        var errors = string.Join("; ", entry.Value.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning("ModelState invalid for {Key}: {Errors}", key, errors);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed while logging relationship validation errors. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Relationship validation logging failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }

            return View(relationship);
        }

        try
        {
            var createdRelationship = await _relationshipService.CreateAsync(relationship, currentUserId, isAdmin);
            await _activityLogService.LogAsync(
                "Relationship Added",
                $"Created a {createdRelationship.RelationshipType} relationship.",
                "FamilyRelationship",
                createdRelationship.Id.ToString(),
                true,
                "A family relationship was added.");

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                try
                {
                    await _notificationService.CreateForUserAndAdminsAsync(
                        currentUserId,
                        "Relationship created",
                        $"You created a {createdRelationship.RelationshipType} relationship.",
                        Url.Action(nameof(Index), "FamilyRelationships"),
                        "FamilyRelationship",
                        createdRelationship.Id,
                        "Success",
                        "bi-diagram-3-fill");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Relationship creation notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                    Console.WriteLine($"[CaughtException] Relationship creation notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
                }
            }

            TempData["SuccessMessage"] = "Relationship saved successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (RelationshipValidationException ex)
        {
            _logger.LogError(ex, "Relationship validation failed during creation. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(relationship);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating relationship");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the relationship. Check logs for details.");
            return View(relationship);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        var relationship = await _relationshipService.GetByIdAsync(id, currentUserId, isAdmin);
        if (relationship is null)
        {
            return NotFound();
        }

        await PopulateViewBagAsync(currentUserId, isAdmin);
        return View(relationship);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FamilyRelationship relationship)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        await PopulateViewBagAsync(currentUserId, isAdmin);

        if (id != relationship.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(relationship);
        }

        try
        {
            await _relationshipService.UpdateAsync(relationship, currentUserId, isAdmin);
            await _activityLogService.LogAsync(
                "Update",
                $"Updated a {relationship.RelationshipType} relationship.",
                "FamilyRelationship",
                relationship.Id.ToString(),
                true,
                "A family relationship was updated.");

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                try
                {
                    await _notificationService.CreateForUserAndAdminsAsync(
                        currentUserId,
                        "Relationship updated",
                        $"You updated a {relationship.RelationshipType} relationship.",
                        Url.Action(nameof(Index), "FamilyRelationships"),
                        "FamilyRelationship",
                        relationship.Id,
                        "Information",
                        "bi-pencil-square");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Relationship update notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                    Console.WriteLine($"[CaughtException] Relationship update notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
                }
            }

            TempData["SuccessMessage"] = "Relationship updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (RelationshipValidationException ex)
        {
            _logger.LogError(ex, "Relationship validation failed during update. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(relationship);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        var relationship = await _relationshipService.GetByIdAsync(id, currentUserId, isAdmin);
        if (relationship is null)
        {
            return NotFound();
        }

        return View(relationship);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = IsCurrentUserAdmin();
        var relationship = await _relationshipService.GetByIdAsync(id, currentUserId, isAdmin);
        await _relationshipService.DeleteAsync(id, currentUserId, isAdmin);
        await _activityLogService.LogAsync(
            "Relationship Deleted",
            relationship is null ? "Deleted a family relationship." : $"Deleted a {relationship.RelationshipType} relationship.",
            "FamilyRelationship",
            id.ToString(),
            true,
            "A family relationship was removed.");

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            try
            {
                await _notificationService.CreateForUserAndAdminsAsync(
                    currentUserId,
                    "Relationship deleted",
                    relationship is null ? "You removed a family relationship." : $"You removed a {relationship.RelationshipType} relationship.",
                    Url.Action(nameof(Index), "FamilyRelationships"),
                    "FamilyRelationship",
                    id,
                    "Warning",
                    "bi-diagram-3-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Relationship deletion notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Relationship deletion notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }
        }
        return RedirectToAction(nameof(Index));
    }

    private bool IsCurrentUserAdmin()
    {
        return User.IsInRole(ApplicationRoles.Administrator) || User.IsInRole(ApplicationRoles.AdminLegacy);
    }

    private async Task PopulateViewBagAsync(string? userId = null, bool isAdmin = false)
    {
        var members = await Task.FromResult(_memberService.GetAll(userId, isAdmin));
        ViewBag.Members = new SelectList(members, "Id", "FullName");
        ViewBag.RelationshipTypes = FamilyRelationship.SupportedRelationshipTypes
            .Select(type => new SelectListItem(type, type))
            .ToList();
    }
}

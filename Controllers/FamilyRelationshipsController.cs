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
        var relationships = _relationshipService.Search(searchTerm);
        ViewBag.SearchTerm = searchTerm;
        return View(relationships);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateViewBagAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FamilyRelationship relationship)
    {
        await PopulateViewBagAsync();

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
            catch
            {
                // swallow logging errors to avoid masking validation issues
            }

            return View(relationship);
        }

        try
        {
            var createdRelationship = await _relationshipService.CreateAsync(relationship);
            await _activityLogService.LogAsync(
                "Relationship Added",
                $"Created a {createdRelationship.RelationshipType} relationship.",
                "FamilyRelationship",
                createdRelationship.Id.ToString(),
                true,
                "A family relationship was added.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                catch
                {
                    // Notification delivery errors should not block the main relationship workflow.
                }
            }

            TempData["SuccessMessage"] = "Relationship saved successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (RelationshipValidationException ex)
        {
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
        var relationship = await _relationshipService.GetByIdAsync(id);
        if (relationship is null)
        {
            return NotFound();
        }

        await PopulateViewBagAsync();
        return View(relationship);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FamilyRelationship relationship)
    {
        await PopulateViewBagAsync();

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
            await _relationshipService.UpdateAsync(relationship);
            await _activityLogService.LogAsync(
                "Update",
                $"Updated a {relationship.RelationshipType} relationship.",
                "FamilyRelationship",
                relationship.Id.ToString(),
                true,
                "A family relationship was updated.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                catch
                {
                    // Notification delivery errors should not block the main relationship workflow.
                }
            }

            TempData["SuccessMessage"] = "Relationship updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (RelationshipValidationException ex)
        {
            ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(relationship);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var relationship = await _relationshipService.GetByIdAsync(id);
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
        var relationship = await _relationshipService.GetByIdAsync(id);
        await _relationshipService.DeleteAsync(id);
        await _activityLogService.LogAsync(
            "Relationship Deleted",
            relationship is null ? "Deleted a family relationship." : $"Deleted a {relationship.RelationshipType} relationship.",
            "FamilyRelationship",
            id.ToString(),
            true,
            "A family relationship was removed.");

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            catch
            {
                // Notification delivery errors should not block the main relationship workflow.
            }
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateViewBagAsync()
    {
        var members = await Task.FromResult(_memberService.GetAll());
        ViewBag.Members = new SelectList(members, "Id", "FullName");
        ViewBag.RelationshipTypes = FamilyRelationship.SupportedRelationshipTypes
            .Select(type => new SelectListItem(type, type))
            .ToList();
    }
}

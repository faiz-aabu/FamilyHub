using System.Security.Claims;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Controllers;

/// <summary>
/// Handles family member pages for the FamilyHub application.
/// </summary>
[Authorize]
public class FamilyMembersController : Controller
{
    private readonly IFamilyMemberService _familyMemberService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;
    private readonly IFamilyRelationshipService _relationshipService;
    private readonly ILogger<FamilyMembersController> _logger;

    /// <summary>
    /// Creates a new family members controller instance.
    /// </summary>
    /// <param name="familyMemberService">The service that provides family member data.</param>
    /// <param name="webHostEnvironment">The environment used to find the web root path.</param>
    public FamilyMembersController(IFamilyMemberService familyMemberService, IWebHostEnvironment webHostEnvironment, IActivityLogService activityLogService, INotificationService notificationService, IFamilyRelationshipService relationshipService, ILogger<FamilyMembersController> logger)
    {
        _familyMemberService = familyMemberService;
        _webHostEnvironment = webHostEnvironment;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
        _relationshipService = relationshipService;
        _logger = logger;
    }

    /// <summary>
    /// Displays all family members in a dashboard view.
    /// </summary>
    /// <returns>The family members page.</returns>
    public async Task<IActionResult> Index(string? searchTerm)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var familyMembers = await _familyMemberService.Search(searchTerm, currentUserId, isAdmin);
        ViewData["SearchTerm"] = searchTerm;

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_MemberCardsPartial", familyMembers);
        }

        return View(familyMembers);
    }

    /// <summary>
    /// Shows the create family member form.
    /// </summary>
    /// <returns>The create page.</returns>
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.RelationshipOptions = GetRelationshipOptions();
        ViewBag.RelatedFamilyMembers = BuildRelatedFamilyMembersList(null, null);
        return View(new FamilyMemberCreateViewModel());
    }

    /// <summary>
    /// Receives the submitted form, validates it, saves the data, and redirects to the family members page.
    /// </summary>
    /// <param name="model">The posted form values.</param>
    /// <returns>The appropriate view or redirect.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] FamilyMemberCreateViewModel model)
    {
        ViewBag.RelationshipOptions = GetRelationshipOptions();
        ViewBag.RelatedFamilyMembers = BuildRelatedFamilyMembersList(null, model.RelatedFamilyMemberId);

        ValidateImageFile(model.ImageFile);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var imagePath = await SaveImageAsync(model.ImageFile, null);

        var familyMember = new FamilyMember
        {
            FirstName = model.FirstName ?? string.Empty,
            LastName = model.LastName ?? string.Empty,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            Occupation = model.Occupation,
            School = model.School,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            Address = model.Address,
            State = model.State,
            Nationality = model.Nationality,
            BloodGroup = model.BloodGroup,
            Genotype = model.Genotype,
            Allergies = model.Allergies,
            Hobbies = model.Hobbies,
            Biography = model.Biography,
            MaritalStatus = model.MaritalStatus,
            Relationship = string.IsNullOrWhiteSpace(model.Relationship) ? null : model.Relationship.Trim(),
            RelatedFamilyMemberId = model.RelatedFamilyMemberId,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var currentUserId = GetCurrentUserId();
        var createdMember = await _familyMemberService.CreateAsync(familyMember, currentUserId);
        await _activityLogService.LogAsync(
            "Member Created",
            $"Created family member record for {createdMember.FullName}.",
            "FamilyMember",
            createdMember.Id.ToString(),
            true,
            "A new family member was added.");

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            try
            {
                await _notificationService.CreateForUserAndAdminsAsync(
                    currentUserId,
                    "Member added",
                    $"You added {createdMember.FullName} to the family directory.",
                    Url.Action(nameof(Details), "FamilyMembers", new { id = createdMember.Id }),
                    "FamilyMember",
                    createdMember.Id,
                    "Success",
                    "bi-person-plus-fill");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Member creation notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                Console.WriteLine($"[CaughtException] Member creation notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            }
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows the edit form for an existing family member.
    /// </summary>
    /// <param name="id">The family member identifier.</param>
    /// <returns>The edit page or not found.</returns>
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var familyMember = await _familyMemberService.GetByIdAsync(id.Value, currentUserId, isAdmin);

        if (familyMember is null)
        {
            return NotFound();
        }

        return View(familyMember);
    }

    /// <summary>
    /// Receives the edited profile data and saves the changes to the database.
    /// </summary>
    /// <param name="id">The family member identifier from the route.</param>
    /// <param name="member">The edited family member model.</param>
    /// <param name="imageFile">An optional new profile image.</param>
    /// <returns>The details page or the edit view when validation fails.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FamilyMember member, IFormFile? imageFile)
    {
        if (id != member.Id)
        {
            return NotFound();
        }

        ValidateImageFile(imageFile);

        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var existingFamilyMember = await _familyMemberService.GetByIdAsync(id, currentUserId, isAdmin);

        if (existingFamilyMember is null)
        {
            return NotFound();
        }

        if (member.RelatedFamilyMemberId == member.Id)
        {
            ModelState.AddModelError(nameof(member.RelatedFamilyMemberId), "A family member cannot be related to themselves.");
        }

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Please correct the highlighted validation errors before saving.");
            member.ImagePath = existingFamilyMember.ImagePath;
            member.CreatedAt = existingFamilyMember.CreatedAt;
            member.UpdatedAt = existingFamilyMember.UpdatedAt;
            ViewBag.RelationshipOptions = GetRelationshipOptions();
            ViewBag.RelatedFamilyMembers = BuildRelatedFamilyMembersList(member.Id, member.RelatedFamilyMemberId);
            return View(member);
        }

        existingFamilyMember.FirstName = member.FirstName ?? string.Empty;
        existingFamilyMember.LastName = member.LastName ?? string.Empty;
        existingFamilyMember.MiddleName = member.MiddleName;
        existingFamilyMember.Nickname = member.Nickname;
        existingFamilyMember.Gender = member.Gender;
        existingFamilyMember.DateOfBirth = member.DateOfBirth;
        existingFamilyMember.PhoneNumber = member.PhoneNumber;
        existingFamilyMember.Email = member.Email;
        existingFamilyMember.Address = member.Address;
        existingFamilyMember.State = member.State;
        existingFamilyMember.Nationality = member.Nationality;
        existingFamilyMember.Occupation = member.Occupation;
        existingFamilyMember.PlaceOfWork = member.PlaceOfWork;
        existingFamilyMember.School = member.School;
        existingFamilyMember.CurrentClass = member.CurrentClass;
        existingFamilyMember.BloodGroup = member.BloodGroup;
        existingFamilyMember.Genotype = member.Genotype;
        existingFamilyMember.Allergies = member.Allergies;
        existingFamilyMember.MedicalConditions = member.MedicalConditions;
        existingFamilyMember.Religion = member.Religion;
        existingFamilyMember.MaritalStatus = member.MaritalStatus;
        existingFamilyMember.Relationship = string.IsNullOrWhiteSpace(member.Relationship) ? null : member.Relationship.Trim();
        existingFamilyMember.RelatedFamilyMemberId = member.RelatedFamilyMemberId;
        existingFamilyMember.Notes = member.Notes;
        existingFamilyMember.Biography = member.Biography;
        existingFamilyMember.FavouriteFood = member.FavouriteFood;
        existingFamilyMember.FavouriteColor = member.FavouriteColor;
        existingFamilyMember.FavouriteQuote = member.FavouriteQuote;
        existingFamilyMember.Hobbies = member.Hobbies;
        existingFamilyMember.ImagePath = await SaveImageAsync(imageFile, existingFamilyMember.ImagePath);
        existingFamilyMember.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _familyMemberService.UpdateAsync(existingFamilyMember, currentUserId, isAdmin);
            await _activityLogService.LogAsync(
                "Member Updated",
                $"Updated family member profile for {existingFamilyMember.FullName}.",
                "FamilyMember",
                existingFamilyMember.Id.ToString(),
                true,
                "Family member details were changed.");

            var notificationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(notificationUserId))
            {
                try
                {
                    await _notificationService.CreateForUserAndAdminsAsync(
                        notificationUserId,
                        "Member updated",
                        $"You updated the profile for {existingFamilyMember.FullName}.",
                        Url.Action(nameof(Details), "FamilyMembers", new { id = existingFamilyMember.Id }),
                        "FamilyMember",
                        existingFamilyMember.Id,
                        "Information",
                        "bi-pencil-square");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Member update notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                    Console.WriteLine($"[CaughtException] Member update notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
                }
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Family member update concurrency failure. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Family member update concurrency failure; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            if (await _familyMemberService.GetByIdAsync(id) is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, "The record was updated by someone else. Please try again.");
            member.ImagePath = existingFamilyMember.ImagePath;
            member.CreatedAt = existingFamilyMember.CreatedAt;
            member.UpdatedAt = existingFamilyMember.UpdatedAt;
            return View(member);
        }

        TempData["SuccessMessage"] = "Member updated successfully.";
        return RedirectToAction(nameof(Details), new { id = existingFamilyMember.Id });
    }

    /// <summary>
    /// Shows a confirmation page before permanently deleting a family member.
    /// </summary>
    /// <param name="id">The family member identifier.</param>
    /// <returns>The delete confirmation page or a not found result.</returns>
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var familyMember = await _familyMemberService.GetByIdAsync(id.Value, currentUserId, isAdmin);

        if (familyMember is null)
        {
            return NotFound();
        }

        var (canDelete, message) = await _relationshipService.GetDeleteImpactAsync(familyMember.Id);
        ViewBag.RelationshipImpact = message;
        ViewBag.CanDelete = canDelete;

        return View(familyMember);
    }

    /// <summary>
    /// Permanently deletes a family member and removes the uploaded profile image when appropriate.
    /// </summary>
    /// <param name="id">The family member identifier.</param>
    /// <returns>A redirect back to the family members list.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var familyMember = await _familyMemberService.GetByIdAsync(id, currentUserId, isAdmin);

        if (familyMember is null)
        {
            return NotFound();
        }

        var (canDelete, message) = await _relationshipService.GetDeleteImpactAsync(familyMember.Id);
        if (!canDelete)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Delete), new { id = familyMember.Id });
        }

        try
        {
            // Delete the uploaded profile image when it exists and is not the default avatar.
            if (!string.IsNullOrWhiteSpace(familyMember.ImagePath)
                && !familyMember.ImagePath.Contains("default-avatar", StringComparison.OrdinalIgnoreCase))
            {
                var imagePath = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    familyMember.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            var memberName = familyMember.FullName;
            await _familyMemberService.DeleteAsync(id);
            await _activityLogService.LogAsync(
                "Member Deleted",
                $"Deleted family member record for {memberName}.",
                "FamilyMember",
                familyMember.Id.ToString(),
                true,
                "A family member record was removed.");

            var notificationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(notificationUserId))
            {
                try
                {
                    await _notificationService.CreateForUserAndAdminsAsync(
                        notificationUserId,
                        "Member deleted",
                        $"You removed {memberName} from the family directory.",
                        Url.Action(nameof(Index), "FamilyMembers"),
                        "FamilyMember",
                        familyMember.Id,
                        "Warning",
                        "bi-person-dash-fill");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Member deletion notification failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
                    Console.WriteLine($"[CaughtException] Member deletion notification failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
                }
            }

            TempData["SuccessMessage"] = "Family member deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Family member deletion failed. User: {User}, Path: {Path}", User.Identity?.Name ?? "Anonymous", Request.Path);
            Console.WriteLine($"[CaughtException] Family member deletion failed; User={User.Identity?.Name ?? "Anonymous"}; Path={Request.Path}{Environment.NewLine}{ex}");
            TempData["ErrorMessage"] = "Unable to delete the selected family member. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Shows the full profile details for one family member.
    /// </summary>
    /// <param name="id">The family member identifier.</param>
    /// <returns>The details view or a friendly not found page.</returns>
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        var familyMember = await _familyMemberService.GetByIdAsync(id, currentUserId, isAdmin);

        if (familyMember is null)
        {
            return View("NotFound");
        }

        return View(familyMember);
    }

    /// <summary>
    /// Saves an uploaded image and returns the image path to store in the database.
    /// </summary>
    /// <param name="imageFile">The uploaded image file.</param>
    /// <param name="existingImagePath">The current image path to preserve when no new image is uploaded.</param>
    /// <returns>The path to use for the family member profile image.</returns>
    private async Task<string> SaveImageAsync(IFormFile? imageFile, string? existingImagePath)
    {
        // If no file was chosen, keep the existing image so the profile does not lose its picture.
        if (imageFile is null || imageFile.Length == 0)
        {
            return existingImagePath ?? "/images/family/default-avatar.png";
        }

        // Validate size before saving.
        const long maxFileSize = 2 * 1024 * 1024; // 2 MB
        if (imageFile.Length > maxFileSize)
        {
            throw new InvalidOperationException("Uploaded image exceeds the maximum allowed size of 2 MB.");
        }

        // Create the uploads folder if it does not exist yet.
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "family");
        Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("Invalid image file extension.");
        }

        // Use a unique file name so new uploads never overwrite an older image by mistake.
        var fileName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        return "/images/family/" + fileName;
    }

    /// <summary>
    /// Builds the relationship dropdown options for the create and edit forms.
    /// </summary>
    /// <returns>A list of bootstrap-friendly select list items.</returns>
    private void ValidateImageFile(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return;
        }

        const long maxFileSize = 2 * 1024 * 1024; // 2 MB
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var allowedContentTypes = new[] { "image/jpeg", "image/png" };

        if (imageFile.Length > maxFileSize)
        {
            ModelState.AddModelError(nameof(imageFile), "Uploaded image must be 2 MB or smaller.");
            return;
        }

        if (!allowedContentTypes.Contains(imageFile.ContentType))
        {
            ModelState.AddModelError(nameof(imageFile), "Only JPEG and PNG images are allowed.");
            return;
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(imageFile), "Only JPEG and PNG images are allowed.");
        }
    }

    private static List<SelectListItem> GetRelationshipOptions()
    {
        return new List<SelectListItem>
        {
            new("Father", "Father"),
            new("Mother", "Mother"),
            new("Son", "Son"),
            new("Daughter", "Daughter"),
            new("Brother", "Brother"),
            new("Sister", "Sister"),
            new("Grandfather", "Grandfather"),
            new("Grandmother", "Grandmother"),
            new("Grandson", "Grandson"),
            new("Granddaughter", "Granddaughter"),
            new("Uncle", "Uncle"),
            new("Aunt", "Aunt"),
            new("Cousin", "Cousin"),
            new("Nephew", "Nephew"),
            new("Niece", "Niece"),
            new("Guardian", "Guardian"),
            new("Other", "Other")
        };
    }

    /// <summary>
    /// Builds the related family member dropdown using all existing family members except the current member.
    /// </summary>
    /// <param name="currentMemberId">The current member identifier, used to exclude the current person from the list.</param>
    /// <param name="selectedValue">The currently selected related family member identifier.</param>
    /// <returns>A select list ready for the form.</returns>
    private IEnumerable<FamilyMember> GetAvailableFamilyMembersForRelationship(int? currentMemberId)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();

        return _familyMemberService.GetAll(currentUserId, isAdmin)
            .Where(member => currentMemberId is null || member.Id != currentMemberId.Value)
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToList();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private bool IsCurrentUserAdmin()
    {
        return User.IsInRole(ApplicationRoles.Administrator) || User.IsInRole(ApplicationRoles.AdminLegacy);
    }

    /// <summary>
    /// Creates a select list of family members for the relationship dropdown.
    /// </summary>
    /// <param name="currentMemberId">The current member identifier.</param>
    /// <param name="selectedValue">The selected related family member identifier.</param>
    /// <returns>A select list used by the Razor views.</returns>
    private SelectList BuildRelatedFamilyMembersList(int? currentMemberId, int? selectedValue)
    {
        var availableMembers = GetAvailableFamilyMembersForRelationship(currentMemberId);
        return new SelectList(availableMembers, nameof(FamilyMember.Id), nameof(FamilyMember.FullName), selectedValue);
    }
}

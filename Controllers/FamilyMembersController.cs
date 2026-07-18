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

    /// <summary>
    /// Creates a new family members controller instance.
    /// </summary>
    /// <param name="familyMemberService">The service that provides family member data.</param>
    /// <param name="webHostEnvironment">The environment used to find the web root path.</param>
    public FamilyMembersController(IFamilyMemberService familyMemberService, IWebHostEnvironment webHostEnvironment)
    {
        _familyMemberService = familyMemberService;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// Displays all family members in a dashboard view.
    /// </summary>
    /// <returns>The family members page.</returns>
    public async Task<IActionResult> Index(string? searchTerm)
    {
        var familyMembers = await _familyMemberService.Search(searchTerm);
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
    public async Task<IActionResult> Create([FromForm] FamilyMemberCreateViewModel model)
    {
        ViewBag.RelationshipOptions = GetRelationshipOptions();
        ViewBag.RelatedFamilyMembers = BuildRelatedFamilyMembersList(null, model.RelatedFamilyMemberId);

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

        await _familyMemberService.CreateAsync(familyMember);
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

        var familyMember = await _familyMemberService.GetByIdAsync(id.Value);

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

        var existingFamilyMember = await _familyMemberService.GetByIdAsync(id);

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
            await _familyMemberService.UpdateAsync(existingFamilyMember);
        }
        catch (DbUpdateConcurrencyException)
        {
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

        var familyMember = await _familyMemberService.GetByIdAsync(id.Value);

        if (familyMember is null)
        {
            return NotFound();
        }

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
        var familyMember = await _familyMemberService.GetByIdAsync(id);

        if (familyMember is null)
        {
            return NotFound();
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

            await _familyMemberService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Family member deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
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
        var familyMember = await _familyMemberService.GetByIdAsync(id);

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

        // Create the uploads folder if it does not exist yet.
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "family");
        Directory.CreateDirectory(uploadsFolder);

        // Use a unique file name so new uploads never overwrite an older image by mistake.
        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
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
        return _familyMemberService.GetAll()
            .Where(member => currentMemberId is null || member.Id != currentMemberId.Value)
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToList();
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

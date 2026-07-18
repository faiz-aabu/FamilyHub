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

    public FamilyRelationshipsController(IFamilyRelationshipService relationshipService, IFamilyMemberService memberService)
    {
        _relationshipService = relationshipService;
        _memberService = memberService;
    }

    public IActionResult Index(string? searchTerm)
    {
        var relationships = _relationshipService.Search(searchTerm);
        ViewBag.SearchTerm = searchTerm;
        return View(relationships);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Members = await GetMemberSelectListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FamilyRelationship relationship)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Members = await GetMemberSelectListAsync();
            return View(relationship);
        }

        if (relationship.MemberId == relationship.RelatedMemberId)
        {
            ModelState.AddModelError(string.Empty, "A person cannot be related to themselves.");
            ViewBag.Members = await GetMemberSelectListAsync();
            return View(relationship);
        }

        await _relationshipService.CreateAsync(relationship);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var relationship = await _relationshipService.GetByIdAsync(id);
        if (relationship is null)
        {
            return NotFound();
        }

        ViewBag.Members = await GetMemberSelectListAsync();
        return View(relationship);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FamilyRelationship relationship)
    {
        if (id != relationship.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Members = await GetMemberSelectListAsync();
            return View(relationship);
        }

        if (relationship.MemberId == relationship.RelatedMemberId)
        {
            ModelState.AddModelError(string.Empty, "A person cannot be related to themselves.");
            ViewBag.Members = await GetMemberSelectListAsync();
            return View(relationship);
        }

        await _relationshipService.UpdateAsync(relationship);
        return RedirectToAction(nameof(Index));
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
        await _relationshipService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private Task<SelectList> GetMemberSelectListAsync()
    {
        var members = _memberService.GetAll();
        return Task.FromResult(new SelectList(members, "Id", "FullName"));
    }
}

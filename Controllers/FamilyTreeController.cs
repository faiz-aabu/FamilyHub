using FamilyHub.Data;
using FamilyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Controllers;

[Authorize]
public class FamilyTreeController : Controller
{
    private readonly FamilyHubDbContext _context;

    public FamilyTreeController(FamilyHubDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var members = await _context.FamilyMembers
            .AsNoTracking()
            .OrderBy(member => member.FirstName)
            .ThenBy(member => member.LastName)
            .ToListAsync();

        var links = await _context.FamilyRelationships
            .AsNoTracking()
            .OrderBy(relation => relation.MemberId)
            .ThenBy(relation => relation.RelatedMemberId)
            .Select(relation => new FamilyTreeLinkViewModel
            {
                MemberId = relation.MemberId,
                RelatedMemberId = relation.RelatedMemberId,
                RelationshipType = relation.RelationshipType
            })
            .ToListAsync();

        var viewModel = new FamilyTreeViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            Members = members,
            Links = links
        };

        return View(viewModel);
    }
}

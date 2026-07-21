using System.Text.Json;
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
        var treeData = await BuildTreeDataAsync();
        var viewModel = new FamilyTreeViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            Members = treeData.Members,
            Links = treeData.Links,
            MembersJson = treeData.MembersJson,
            LinksJson = treeData.LinksJson
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Data()
    {
        var treeData = await BuildTreeDataAsync();
        return Json(new
        {
            members = treeData.MemberPayloads,
            links = treeData.LinkPayloads,
            roots = treeData.RootIds
        });
    }

    private async Task<TreeDataResult> BuildTreeDataAsync()
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
            .ToListAsync();

        var memberById = members.ToDictionary(member => member.Id, member => member);
        var normalizedLinks = new List<FamilyTreeLinkViewModel>();
        var parentChildren = new Dictionary<int, List<int>>();
        var childParents = new Dictionary<int, List<int>>();

        foreach (var relation in links)
        {
            if (!memberById.ContainsKey(relation.MemberId) || !memberById.ContainsKey(relation.RelatedMemberId))
            {
                continue;
            }

            var normalizedType = NormalizeRelationshipType(relation.RelationshipType);
            switch (normalizedType)
            {
                case "parent":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = normalizedType
                    });
                    AddEdge(parentChildren, childParents, relation.MemberId, relation.RelatedMemberId);
                    break;
                case "child":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.RelatedMemberId,
                        RelatedMemberId = relation.MemberId,
                        RelationshipType = "parent"
                    });
                    AddEdge(parentChildren, childParents, relation.RelatedMemberId, relation.MemberId);
                    break;
                case "grandparent":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "parent"
                    });
                    AddEdge(parentChildren, childParents, relation.MemberId, relation.RelatedMemberId);
                    break;
                case "grandchild":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.RelatedMemberId,
                        RelatedMemberId = relation.MemberId,
                        RelationshipType = "parent"
                    });
                    AddEdge(parentChildren, childParents, relation.RelatedMemberId, relation.MemberId);
                    break;
                case "spouse":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "spouse"
                    });
                    break;
                case "sibling":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "sibling"
                    });
                    break;
                case "aunt":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "aunt"
                    });
                    break;
                case "uncle":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "uncle"
                    });
                    break;
                case "cousin":
                    normalizedLinks.Add(new FamilyTreeLinkViewModel
                    {
                        MemberId = relation.MemberId,
                        RelatedMemberId = relation.RelatedMemberId,
                        RelationshipType = "cousin"
                    });
                    break;
            }
        }

        var generations = new Dictionary<int, int>();
        var queue = new Queue<int>();
        var memberIds = memberById.Keys.ToList();
        foreach (var memberId in memberIds)
        {
            if (!childParents.ContainsKey(memberId))
            {
                generations[memberId] = 0;
                queue.Enqueue(memberId);
            }
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!parentChildren.TryGetValue(currentId, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                if (!generations.ContainsKey(childId))
                {
                    generations[childId] = generations[currentId] + 1;
                    queue.Enqueue(childId);
                }
            }
        }

        foreach (var memberId in memberIds)
        {
            generations.TryAdd(memberId, 0);
        }

        var memberPayloads = members.Select(member => new TreeNodeDto
        {
            Id = member.Id,
            Name = member.FullName,
            Gender = string.IsNullOrWhiteSpace(member.Gender) ? "Unknown" : member.Gender,
            Age = member.Age?.ToString() ?? "Unknown",
            Photo = string.IsNullOrWhiteSpace(member.ImagePath) ? "/images/family/default-avatar.png" : member.ImagePath,
            RelationshipLabel = ResolveRelationshipLabel(member),
            Generation = generations.GetValueOrDefault(member.Id, 0),
            ParentIds = (childParents.TryGetValue(member.Id, out var parents) ? parents : new List<int>()).Distinct().ToList(),
            ProfileUrl = Url.Action("Details", "FamilyMembers", new { id = member.Id })
        }).ToList();

        var linkPayloads = normalizedLinks.Select(link => new TreeLinkDto
        {
            Source = link.MemberId,
            Target = link.RelatedMemberId,
            Type = link.RelationshipType
        }).ToList();

        var rootIds = memberPayloads
            .Where(member => member.Generation == 0)
            .Select(member => member.Id)
            .OrderBy(id => id)
            .ToList();

        return new TreeDataResult
        {
            Members = members,
            Links = normalizedLinks,
            MemberPayloads = memberPayloads,
            LinkPayloads = linkPayloads,
            RootIds = rootIds,
            MembersJson = JsonSerializer.Serialize(memberPayloads),
            LinksJson = JsonSerializer.Serialize(linkPayloads)
        };
    }

    private static string NormalizeRelationshipType(string? relationshipType)
    {
        if (string.IsNullOrWhiteSpace(relationshipType))
        {
            return "other";
        }

        var normalized = relationshipType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "father" or "mother" or "parent" => "parent",
            "son" or "daughter" or "child" => "child",
            "grandfather" or "grandmother" or "grandparent" => "grandparent",
            "grandchild" => "grandchild",
            "spouse" => "spouse",
            "sibling" or "brother" or "sister" => "sibling",
            "aunt" => "aunt",
            "uncle" => "uncle",
            "cousin" => "cousin",
            _ => "other"
        };
    }

    private static string ResolveRelationshipLabel(FamilyMember member)
    {
        if (!string.IsNullOrWhiteSpace(member.Relationship))
        {
            return member.Relationship;
        }

        return member.Gender is "Male" or "Female" ? member.Gender : "Member";
    }

    private static void AddEdge(Dictionary<int, List<int>> parentChildren, Dictionary<int, List<int>> childParents, int parentId, int childId)
    {
        if (!parentChildren.ContainsKey(parentId))
        {
            parentChildren[parentId] = new List<int>();
        }

        if (!childParents.ContainsKey(childId))
        {
            childParents[childId] = new List<int>();
        }

        if (!parentChildren[parentId].Contains(childId))
        {
            parentChildren[parentId].Add(childId);
        }

        if (!childParents[childId].Contains(parentId))
        {
            childParents[childId].Add(parentId);
        }
    }

    private sealed class TreeDataResult
    {
        public IReadOnlyList<FamilyMember> Members { get; set; } = Array.Empty<FamilyMember>();

        public IReadOnlyList<FamilyTreeLinkViewModel> Links { get; set; } = Array.Empty<FamilyTreeLinkViewModel>();

        public List<TreeNodeDto> MemberPayloads { get; set; } = new();

        public List<TreeLinkDto> LinkPayloads { get; set; } = new();

        public List<int> RootIds { get; set; } = new();

        public string MembersJson { get; set; } = "[]";

        public string LinksJson { get; set; } = "[]";
    }

    private sealed class TreeNodeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public string Age { get; set; } = string.Empty;

        public string Photo { get; set; } = string.Empty;

        public string RelationshipLabel { get; set; } = string.Empty;

        public int Generation { get; set; }

        public List<int> ParentIds { get; set; } = new();

        public string? ProfileUrl { get; set; }
    }

    private sealed class TreeLinkDto
    {
        public int Source { get; set; }

        public int Target { get; set; }

        public string Type { get; set; } = string.Empty;
    }
}

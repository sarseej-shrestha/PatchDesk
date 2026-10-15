using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.Controllers;
[ApiController, Route("api")]
public class WorkspaceController(PatchDeskDbContext db) : ControllerBase
{
    [HttpGet("developers")]
    public async Task<DeveloperDto[]> Developers(CancellationToken ct) => await db.Developers.OrderBy(d => d.Id).Select(d => new DeveloperDto(d.Id, d.Name, d.Email, d.Role)).ToArrayAsync(ct);
    [HttpGet("activity")]
    public async Task<PageDto<ActivityDto>> Activity([FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100) throw new Middleware.ApiException(400, "Invalid pagination.");
        var events = await db.Activities.AsNoTracking().Include(a => a.Actor).OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return new(events.Select(a => a.ToDto()).ToArray(), await db.Activities.CountAsync(ct), page, pageSize);
    }
    [HttpGet("dashboard/summary")]
    public async Task<object> Summary(CancellationToken ct)
    {
        var rows = await db.Issues.Select(i => new { i.Status, i.Priority, i.IssueType }).ToArrayAsync(ct);
        var recent = await db.Activities.AsNoTracking().Include(a => a.Actor).OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(8).ToArrayAsync(ct);
        return new { total = rows.Length, open = rows.Count(i => i.Status == IssueStatus.Open), inProgress = rows.Count(i => i.Status == IssueStatus.InProgress), inReview = rows.Count(i => i.Status == IssueStatus.InReview), resolved = rows.Count(i => i.Status == IssueStatus.Resolved), reopened = rows.Count(i => i.Status == IssueStatus.Reopened), criticalBugs = rows.Count(i => i.IssueType == IssueType.Bug && i.Priority == Priority.Critical && i.Status != IssueStatus.Resolved), recentActivity = recent.Select(a => a.ToDto()) };
    }
}

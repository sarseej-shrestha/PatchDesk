using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Middleware;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.Services;

public class IssueService(PatchDeskDbContext db)
{
    public IQueryable<Issue> Details => db.Issues.Include(i => i.Reporter).Include(i => i.Assignee)
        .Include(i => i.Comments).ThenInclude(c => c.Author)
        .Include(i => i.Activities).ThenInclude(a => a.Actor)
        .Include(i => i.RegressionTests).ThenInclude(t => t.Executions).Include(i => i.FixReferences).AsSplitQuery();
    public async Task<Issue> Get(int id, CancellationToken ct = default) => await Details.SingleOrDefaultAsync(i => i.Id == id, ct) ?? throw new ApiException(404, "Issue not found.");
    public async Task RequireActor(int id, CancellationToken ct = default)
    {
        if (await db.Developers.FindAsync([id], ct) == null) throw new ApiException(400, "Select an existing demo actor.");
    }
    public static void RequireVersion(Issue issue, int version)
    {
        if (issue.Version != version) throw new ApiException(409, "This issue changed. Refresh it and retry with the current version.");
    }
    public static void RequireEditable(Issue issue)
    {
        if (issue.Status == IssueStatus.Resolved) throw new ApiException(409, "Reopen the issue before changing its engineering evidence or fields.");
    }
    public static void Record(Issue issue, int actorId, string action, string? previous, string? next, bool invalidateEvidence = false)
    {
        issue.Version++;
        issue.UpdatedAt = DateTime.UtcNow;
        if (invalidateEvidence) issue.EvidenceRevision++;
        issue.Activities.Add(new IssueActivity { ActorId = actorId, Action = action, PreviousValue = previous, NewValue = next });
    }
    public async Task<PageDto<IssueDto>> List(IssueQuery query, CancellationToken ct)
    {
        if (query.Page < 1 || query.Page > 100000 || query.PageSize is < 1 or > 100 || query.Search?.Length > 200 ||
            query.Status.HasValue && !Enum.IsDefined(query.Status.Value) || query.Priority.HasValue && !Enum.IsDefined(query.Priority.Value) || query.AssigneeId <= 0)
            throw new ApiException(400, "Invalid pagination or filter. Page size must be between 1 and 100.");
        var q = db.Issues.AsNoTracking().Include(i => i.Reporter).Include(i => i.Assignee).AsQueryable();
        if (query.Status.HasValue) q = q.Where(i => i.Status == query.Status);
        if (query.Priority.HasValue) q = q.Where(i => i.Priority == query.Priority);
        if (query.AssigneeId.HasValue) q = q.Where(i => i.AssigneeId == query.AssigneeId);
        if (!string.IsNullOrWhiteSpace(query.Search)) { var term = query.Search.Trim().ToLower(); q = q.Where(i => i.Title.ToLower().Contains(term) || i.Description.ToLower().Contains(term)); }
        var total = await q.CountAsync(ct);
        q = query.Sort switch {
            "newest" => q.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id),
            "oldest" => q.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id),
            "priority" => q.OrderByDescending(i => i.Priority).ThenByDescending(i => i.Id),
            _ => throw new ApiException(400, "Sort must be newest, oldest, or priority.")
        };
        return new((await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct)).Select(i => i.ToDto()).ToArray(), total, query.Page, query.PageSize);
    }
    public async Task<IssueDto> Create(CreateIssueRequest r, CancellationToken ct)
    {
        await RequireActor(r.ReporterId, ct);
        if (r.AssigneeId.HasValue) await RequireActor(r.AssigneeId.Value, ct);
        var i = new Issue { Title = r.Title.Trim(), Description = r.Description.Trim(), ReproductionSteps = r.ReproductionSteps?.Trim() ?? "", IssueType = r.IssueType, Priority = r.Priority, Severity = r.Severity, ReporterId = r.ReporterId, AssigneeId = r.AssigneeId };
        i.Activities.Add(new IssueActivity { ActorId = r.ReporterId, Action = "Created", NewValue = i.Title });
        db.Issues.Add(i);
        await db.SaveChangesAsync(ct);
        return (await Get(i.Id, ct)).ToDto();
    }
    public async Task<IssueDto> Update(int id, UpdateIssueRequest r, CancellationToken ct)
    {
        var i = await Get(id, ct); RequireVersion(i, r.Version); RequireEditable(i); await RequireActor(r.ActorId, ct);
        var before = $"{i.Title}; {i.Priority}; {i.Severity}";
        i.Title = r.Title.Trim(); i.Description = r.Description.Trim(); i.ReproductionSteps = r.ReproductionSteps?.Trim() ?? ""; i.Priority = r.Priority; i.Severity = r.Severity;
        Record(i, r.ActorId, "Updated", before, $"{i.Title}; {i.Priority}; {i.Severity}", true);
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
    public async Task<IssueDto> Assign(int id, AssignRequest r, CancellationToken ct)
    {
        var i = await Get(id, ct); RequireVersion(i, r.Version); RequireEditable(i); await RequireActor(r.ActorId, ct);
        if (r.AssigneeId.HasValue) await RequireActor(r.AssigneeId.Value, ct);
        if (r.AssigneeId == null && i.Status == IssueStatus.InReview) throw new ApiException(409, "An issue in review must retain an assignee.");
        var before = i.AssigneeId?.ToString(); i.AssigneeId = r.AssigneeId;
        i.Assignee = r.AssigneeId.HasValue ? await db.Developers.FindAsync([r.AssigneeId.Value], ct) : null;
        Record(i, r.ActorId, "Assigned", before, r.AssigneeId?.ToString());
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
    public async Task<CommentDto> Comment(int id, CommentRequest r, CancellationToken ct)
    {
        var i = await Get(id, ct); RequireVersion(i, r.Version); await RequireActor(r.ActorId, ct);
        var comment = new IssueComment { AuthorId = r.ActorId, Content = r.Content.Trim() };
        i.Comments.Add(comment); Record(i, r.ActorId, "Commented", null, comment.Content);
        await db.SaveChangesAsync(ct);
        return (await Get(id, ct)).ToDto().Comments.Single(c => c.Id == comment.Id);
    }
    public async Task Delete(int id, int actorId, int version, CancellationToken ct)
    {
        var i = await Get(id, ct); RequireVersion(i, version); RequireEditable(i); await RequireActor(actorId, ct);
        db.Issues.Remove(i); await db.SaveChangesAsync(ct);
    }
}

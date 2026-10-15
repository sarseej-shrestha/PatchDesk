using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.Services;

public class WorkflowService(PatchDeskDbContext db, IssueService issues)
{
    public async Task<IssueDto> Transition(int id, TransitionRequest r, CancellationToken ct)
    {
        var i = await issues.Get(id, ct); IssueService.RequireVersion(i, r.Version); await issues.RequireActor(r.ActorId, ct);
        var actor = await db.Developers.SingleAsync(a => a.Id == r.ActorId, ct);
        WorkflowEngine.Validate(i, r.Status, r.ResolutionNote, r.ManualOverride, r.OverrideReason, actor.Role);
        var before = i.Status;
        i.Status = r.Status;
        i.ResolvedAt = r.Status == IssueStatus.Resolved ? DateTime.UtcNow : null;
        if (r.Status == IssueStatus.Resolved) { i.ResolutionNote = r.ResolutionNote!.Trim(); i.ResolutionOverridden = r.ManualOverride; }
        if (r.Status == IssueStatus.Reopened) { i.ResolutionNote = null; i.ResolutionOverridden = false; }
        IssueService.Record(i, r.ActorId, r.ManualOverride ? "ResolvedWithManualOverride" : "StatusChanged", before.ToString(),
            $"{r.Status}" + (r.Status == IssueStatus.Resolved ? $": {r.ResolutionNote}" : "") + (r.ManualOverride ? $"; Override reason: {r.OverrideReason}" : ""),
            r.Status is IssueStatus.Reopened or IssueStatus.Open || before == IssueStatus.InReview && r.Status == IssueStatus.InProgress);
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
}

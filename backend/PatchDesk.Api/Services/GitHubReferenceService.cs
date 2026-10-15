using System.Text.RegularExpressions;
using PatchDesk.Api.Data;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Middleware;
namespace PatchDesk.Api.Services;
public class GitHubReferenceService(PatchDeskDbContext db, IssueService issues)
{
    public const string Repository = "https://github.com/sarseej-shrestha/PatchDesk";
    public static void Validate(FixRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.BranchName) && string.IsNullOrWhiteSpace(r.CommitHash) && string.IsNullOrWhiteSpace(r.PullRequestUrl)) throw new ApiException(400, "Provide a branch, full commit hash, or pull request URL.");
        if (r.CommitHash is not null && !Regex.IsMatch(r.CommitHash, "\\A[0-9a-fA-F]{40}\\z")) throw new ApiException(400, "Commit hashes must contain exactly 40 hexadecimal characters.");
        if (r.BranchName is not null && (r.BranchName.Length > 200 || !Regex.IsMatch(r.BranchName, "\\A[A-Za-z0-9][A-Za-z0-9._/-]*\\z") || r.BranchName.Contains("..") || r.BranchName.Split('/').Any(s => s.Length == 0 || s.StartsWith('.') || s.EndsWith('.') || s.EndsWith(".lock")))) throw new ApiException(400, "Invalid Git branch name.");
        if (r.PullRequestUrl is not null && !Regex.IsMatch(r.PullRequestUrl, "\\Ahttps://github\\.com/sarseej-shrestha/PatchDesk/pull/[1-9][0-9]*\\z")) throw new ApiException(400, "Pull requests must be HTTPS URLs in the PatchDesk repository.");
    }
    public async Task<IssueDto> Add(int id, FixRequest r, CancellationToken ct)
    {
        Validate(r);
        var i = await issues.Get(id, ct); IssueService.RequireVersion(i, r.Version); IssueService.RequireEditable(i); await issues.RequireActor(r.ActorId, ct);
        i.FixReferences.Add(new Models.FixReference { BranchName = r.BranchName, CommitHash = r.CommitHash?.ToLowerInvariant(), PullRequestUrl = r.PullRequestUrl, Description = r.Description.Trim() });
        IssueService.Record(i, r.ActorId, "FixLinked", null, r.CommitHash ?? r.PullRequestUrl ?? r.BranchName, true);
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
}

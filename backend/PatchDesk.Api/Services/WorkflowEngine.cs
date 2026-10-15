using PatchDesk.Api.Models;
using PatchDesk.Api.Middleware;
namespace PatchDesk.Api.Services;

public static class WorkflowEngine
{
    private static readonly IReadOnlyDictionary<IssueStatus, IssueStatus[]> Transitions = new Dictionary<IssueStatus, IssueStatus[]> {
        [IssueStatus.Open] = [IssueStatus.InProgress],
        [IssueStatus.InProgress] = [IssueStatus.InReview, IssueStatus.Open],
        [IssueStatus.InReview] = [IssueStatus.Resolved, IssueStatus.InProgress, IssueStatus.Open],
        [IssueStatus.Resolved] = [IssueStatus.Reopened],
        [IssueStatus.Reopened] = [IssueStatus.InProgress, IssueStatus.Open]
    };
    public static IReadOnlyList<IssueStatus> Allowed(IssueStatus status) => Transitions[status];
    public static bool HasCurrentEvidence(Issue issue)
    {
        var fix = issue.FixReferences.OrderByDescending(f => f.Id).FirstOrDefault();
        if (fix?.CommitHash is not { Length: 40 } || issue.RegressionTests.Count == 0) return false;
        return issue.RegressionTests.All(t => {
            var e = t.Executions.OrderByDescending(e => e.Id).FirstOrDefault();
            return e is { Result: TestResult.Passed, IsDefectiveFixture: false }
                && e.ExecutionSource == "PatchDesk controlled registry"
                && e.EvidenceRevision == issue.EvidenceRevision
                && string.Equals(e.CommitHash, fix.CommitHash, StringComparison.OrdinalIgnoreCase);
        });
    }
    public static void Validate(Issue issue, IssueStatus next, string? note, bool manualOverride, string? overrideReason, string actorRole)
    {
        if (!Allowed(issue.Status).Contains(next)) throw new ApiException(409, $"Cannot transition from {issue.Status} to {next}.");
        if (next == IssueStatus.InReview && issue.AssigneeId == null) throw new ApiException(409, "Assign an engineer before review.");
        if (manualOverride && next != IssueStatus.Resolved) throw new ApiException(400, "Manual override is only available when resolving.");
        if (next == IssueStatus.Resolved)
        {
            if (string.IsNullOrWhiteSpace(note)) throw new ApiException(400, "A resolution note is required.");
            if (actorRole != "Reviewer") throw new ApiException(409, "Select a demo reviewer to resolve this issue. Actor selection is simulated, not authentication.");
            if (manualOverride && string.IsNullOrWhiteSpace(overrideReason)) throw new ApiException(400, "Manual override requires an explicit reason.");
            if (issue.IssueType == IssueType.Bug && !manualOverride && !HasCurrentEvidence(issue))
                throw new ApiException(409, "Verified bug resolution requires current passing executions for every associated test, matching the latest linked fix commit. Execute the corrected fixture after all issue and fix changes.");
        }
    }
}

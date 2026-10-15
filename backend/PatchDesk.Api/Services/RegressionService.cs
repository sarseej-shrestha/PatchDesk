using PatchDesk.Api.Data;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Middleware;
using PatchDesk.Api.Models;
using PatchDesk.Api.Regression;
namespace PatchDesk.Api.Services;
public class RegressionService(PatchDeskDbContext db, IssueService issues, IBuildIdentity build)
{
    public async Task<IssueDto> Associate(int id, AssociateTestRequest r, CancellationToken ct)
    {
        var i = await issues.Get(id, ct); IssueService.RequireVersion(i, r.Version); IssueService.RequireEditable(i); await issues.RequireActor(r.ActorId, ct);
        var definition = RegressionRegistry.Tests.SingleOrDefault(t => t.Identifier == r.TestIdentifier) ?? throw new ApiException(400, "Only registered repository tests may be associated.");
        if (i.RegressionTests.Any(t => t.TestIdentifier == r.TestIdentifier)) throw new ApiException(409, "This test is already associated.");
        i.RegressionTests.Add(new RegressionTest { TestIdentifier = definition.Identifier, TestName = definition.Name, Description = definition.Description });
        IssueService.Record(i, r.ActorId, "TestAssociated", null, definition.Identifier, true);
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
    public async Task<IssueDto> Execute(int id, int testId, ExecuteTestRequest r, CancellationToken ct)
    {
        var i = await issues.Get(id, ct); IssueService.RequireVersion(i, r.Version); IssueService.RequireEditable(i); await issues.RequireActor(r.ActorId, ct);
        var test = i.RegressionTests.SingleOrDefault(t => t.Id == testId) ?? throw new ApiException(404, "Associated regression test not found.");
        var result = RegressionRegistry.Run(test.TestIdentifier, r.DefectiveFixture);
        var execution = new TestExecution {
            Result = result.Passed ? TestResult.Passed : TestResult.Failed,
            ExecutionSource = "PatchDesk controlled registry", CommitHash = build.IsClean ? build.Commit : "",
            DurationMilliseconds = result.DurationMilliseconds, Evidence = $"{result.Evidence}; assembly-sha256={build.AssemblySha256}; clean-checkout={build.IsClean}",
            EvidenceRevision = i.EvidenceRevision, IsDefectiveFixture = r.DefectiveFixture
        };
        test.Executions.Add(execution); test.LatestResult = execution.Result; test.LastExecutedAt = execution.ExecutedAt;
        IssueService.Record(i, r.ActorId, "TestExecuted", null, $"{test.TestIdentifier}: {execution.Result} ({(r.DefectiveFixture ? "defective fixture" : "corrected implementation")})");
        await db.SaveChangesAsync(ct); return i.ToDto();
    }
}

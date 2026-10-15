using PatchDesk.Api.Models;
using PatchDesk.Api.Services;
using PatchDesk.Api.Middleware;
namespace PatchDesk.Tests.Unit;
public class WorkflowTests
{
    private static Issue Issue(IssueStatus status) => new() { Title = "Boundary defect", Description = "Discount", ReporterId = 1, AssigneeId = 2, Status = status };
    [Theory]
    [InlineData(IssueStatus.Open, IssueStatus.InProgress)]
    [InlineData(IssueStatus.InProgress, IssueStatus.InReview)]
    [InlineData(IssueStatus.InReview, IssueStatus.InProgress)]
    [InlineData(IssueStatus.Resolved, IssueStatus.Reopened)]
    [InlineData(IssueStatus.Reopened, IssueStatus.InProgress)]
    public void LegalTransitionsAreAccepted(IssueStatus from, IssueStatus to) => WorkflowEngine.Validate(Issue(from), to, null, false, null, "Engineer");
    [Theory]
    [InlineData(IssueStatus.Open, IssueStatus.Resolved)]
    [InlineData(IssueStatus.Resolved, IssueStatus.InProgress)]
    [InlineData(IssueStatus.Open, IssueStatus.Open)]
    public void InvalidTransitionsAreRejected(IssueStatus from, IssueStatus to) => Assert.Throws<ApiException>(() => WorkflowEngine.Validate(Issue(from), to, "Done", false, null, "Reviewer"));
    [Fact]
    public void ReviewRequiresAnAssignee()
    {
        var i = Issue(IssueStatus.InProgress); i.AssigneeId = null;
        Assert.Throws<ApiException>(() => WorkflowEngine.Validate(i, IssueStatus.InReview, null, false, null, "Reviewer"));
    }
    [Fact]
    public void BugResolutionRequiresEvidenceOrExplicitOverride()
    {
        var i = Issue(IssueStatus.InReview);
        Assert.Throws<ApiException>(() => WorkflowEngine.Validate(i, IssueStatus.Resolved, "Done", false, null, "Reviewer"));
        Assert.Throws<ApiException>(() => WorkflowEngine.Validate(i, IssueStatus.Resolved, "Done", true, " ", "Reviewer"));
        Assert.Throws<ApiException>(() => WorkflowEngine.Validate(i, IssueStatus.Resolved, null, true, "Prototype exception", "Reviewer"));
        WorkflowEngine.Validate(i, IssueStatus.Resolved, "Manually reviewed", true, "External behavior cannot be tested by registry", "Reviewer");
    }
    [Fact]
    public void EvidenceMustMatchCurrentRevisionLatestRunAndCommit()
    {
        var i = Issue(IssueStatus.InReview); var sha = new string('a', 40);
        i.FixReferences.Add(new FixReference { CommitHash = sha, Description = "Fix" });
        var execution = new TestExecution { Result = TestResult.Passed, ExecutionSource = "PatchDesk controlled registry", CommitHash = sha, Evidence = "90 equals 90", EvidenceRevision = 1 };
        i.RegressionTests.Add(new RegressionTest { TestName = "Boundary", Description = "Expected 90", TestIdentifier = "discount.boundary", Executions = [execution] });
        Assert.True(WorkflowEngine.HasCurrentEvidence(i));
        execution.IsDefectiveFixture = true; Assert.False(WorkflowEngine.HasCurrentEvidence(i));
        execution.IsDefectiveFixture = false; i.EvidenceRevision++; Assert.False(WorkflowEngine.HasCurrentEvidence(i));
        i.EvidenceRevision--; execution.CommitHash = new string('b', 40); Assert.False(WorkflowEngine.HasCurrentEvidence(i));
        execution.CommitHash = sha; execution.Result = TestResult.Failed; Assert.False(WorkflowEngine.HasCurrentEvidence(i));
    }
}

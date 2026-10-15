namespace PatchDesk.Api.Models;

public enum IssueType { Bug, Feature, Improvement, Maintenance }
public enum Priority { Low, Medium, High, Critical }
public enum Severity { Minor, Moderate, Major, Critical }
public enum IssueStatus { Open, InProgress, InReview, Resolved, Reopened }
public enum TestResult { NotRun, Passed, Failed }

public class Developer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Issue
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string ReproductionSteps { get; set; } = "";
    public IssueType IssueType { get; set; }
    public Priority Priority { get; set; }
    public Severity Severity { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Open;
    public int? AssigneeId { get; set; }
    public Developer? Assignee { get; set; }
    public int ReporterId { get; set; }
    public Developer Reporter { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public bool ResolutionOverridden { get; set; }
    public int Version { get; set; } = 1;
    public int EvidenceRevision { get; set; } = 1;
    public List<IssueComment> Comments { get; set; } = [];
    public List<IssueActivity> Activities { get; set; } = [];
    public List<RegressionTest> RegressionTests { get; set; } = [];
    public List<FixReference> FixReferences { get; set; } = [];
}
public class IssueComment
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public int AuthorId { get; set; }
    public Developer Author { get; set; } = null!;
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class RegressionTest
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public required string TestName { get; set; }
    public required string Description { get; set; }
    public required string TestIdentifier { get; set; }
    public string Framework { get; set; } = "PatchDesk controlled registry (shared xUnit assertions)";
    public TestResult LatestResult { get; set; } = TestResult.NotRun;
    public DateTime? LastExecutedAt { get; set; }
    public List<TestExecution> Executions { get; set; } = [];
}
public class TestExecution
{
    public int Id { get; set; }
    public int RegressionTestId { get; set; }
    public TestResult Result { get; set; }
    public required string ExecutionSource { get; set; }
    public required string CommitHash { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public long DurationMilliseconds { get; set; }
    public required string Evidence { get; set; }
    public int EvidenceRevision { get; set; }
    public bool IsDefectiveFixture { get; set; }
}
public class FixReference
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string? BranchName { get; set; }
    public string? CommitHash { get; set; }
    public string? PullRequestUrl { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class IssueActivity
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public int ActorId { get; set; }
    public Developer Actor { get; set; } = null!;
    public required string Action { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

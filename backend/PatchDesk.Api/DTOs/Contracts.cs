using System.ComponentModel.DataAnnotations;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.DTOs;

public record CreateIssueRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(10000)] string Description,
    [StringLength(10000)] string ReproductionSteps,
    [EnumDataType(typeof(IssueType))] IssueType IssueType,
    [EnumDataType(typeof(Priority))] Priority Priority,
    [EnumDataType(typeof(Severity))] Severity Severity,
    [Range(1, int.MaxValue)] int ReporterId,
    [Range(1, int.MaxValue)] int? AssigneeId);
public record UpdateIssueRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(10000)] string Description,
    [StringLength(10000)] string ReproductionSteps,
    [EnumDataType(typeof(Priority))] Priority Priority,
    [EnumDataType(typeof(Severity))] Severity Severity,
    [Range(1, int.MaxValue)] int ActorId,
    [Range(1, int.MaxValue)] int Version);
public record AssignRequest([Range(1, int.MaxValue)] int? AssigneeId, [Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version);
public record CommentRequest([Required, StringLength(5000)] string Content, [Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version);
public record IssueQuery(int Page = 1, int PageSize = 20, IssueStatus? Status = null, Priority? Priority = null, int? AssigneeId = null, string? Search = null, string Sort = "newest");
public record DeveloperDto(int Id, string Name, string Email, string Role);
public record CommentDto(int Id, int AuthorId, string Author, string Content, DateTime CreatedAt);
public record ActivityDto(int Id, int IssueId, int ActorId, string Actor, string Action, string? PreviousValue, string? NewValue, DateTime Timestamp);
public record ExecutionDto(int Id, TestResult Result, string ExecutionSource, string CommitHash, DateTime ExecutedAt, long DurationMilliseconds, string Evidence, int EvidenceRevision, bool IsDefectiveFixture);
public record RegressionDto(int Id, string TestName, string Description, string TestIdentifier, string Framework, TestResult LatestResult, DateTime? LastExecutedAt, IReadOnlyList<ExecutionDto> Executions);
public record FixDto(int Id, string? BranchName, string? CommitHash, string? PullRequestUrl, string Description, DateTime CreatedAt);
public record IssueDto(int Id, string Title, string Description, string ReproductionSteps, IssueType IssueType, Priority Priority, Severity Severity, IssueStatus Status, int ReporterId, string Reporter, int? AssigneeId, string? Assignee, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ResolvedAt, string? ResolutionNote, bool ResolutionOverridden, int Version, int EvidenceRevision, IReadOnlyList<CommentDto> Comments, IReadOnlyList<ActivityDto> Activities, IReadOnlyList<RegressionDto> RegressionTests, IReadOnlyList<FixDto> FixReferences);
public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public static class DtoMapper
{
    public static ActivityDto ToDto(this IssueActivity a) => new(a.Id, a.IssueId, a.ActorId, a.Actor.Name, a.Action, a.PreviousValue, a.NewValue, a.Timestamp);
    public static IssueDto ToDto(this Issue i) => new(i.Id, i.Title, i.Description, i.ReproductionSteps, i.IssueType, i.Priority, i.Severity, i.Status, i.ReporterId, i.Reporter.Name, i.AssigneeId, i.Assignee?.Name, i.CreatedAt, i.UpdatedAt, i.ResolvedAt, i.ResolutionNote, i.ResolutionOverridden, i.Version, i.EvidenceRevision,
        i.Comments.OrderBy(c => c.Id).Select(c => new CommentDto(c.Id, c.AuthorId, c.Author.Name, c.Content, c.CreatedAt)).ToArray(),
        i.Activities.OrderByDescending(a => a.Id).Select(a => a.ToDto()).ToArray(),
        i.RegressionTests.Select(t => new RegressionDto(t.Id, t.TestName, t.Description, t.TestIdentifier, t.Framework, t.LatestResult, t.LastExecutedAt, t.Executions.OrderByDescending(e => e.Id).Select(e => new ExecutionDto(e.Id, e.Result, e.ExecutionSource, e.CommitHash, e.ExecutedAt, e.DurationMilliseconds, e.Evidence, e.EvidenceRevision, e.IsDefectiveFixture)).ToArray())).ToArray(),
        i.FixReferences.OrderByDescending(f => f.Id).Select(f => new FixDto(f.Id, f.BranchName, f.CommitHash, f.PullRequestUrl, f.Description, f.CreatedAt)).ToArray());
}
public record TransitionRequest(
    [EnumDataType(typeof(IssueStatus))] IssueStatus Status,
    [Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version,
    [StringLength(5000)] string? ResolutionNote = null, bool ManualOverride = false, [StringLength(5000)] string? OverrideReason = null);
public record AssociateTestRequest([Required, StringLength(100)] string TestIdentifier, [Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version);
public record ExecuteTestRequest([Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version, bool DefectiveFixture = false);
public record FixRequest([StringLength(200)] string? BranchName, [StringLength(40)] string? CommitHash, [StringLength(500)] string? PullRequestUrl, [Required, StringLength(2000)] string Description, [Range(1, int.MaxValue)] int ActorId, [Range(1, int.MaxValue)] int Version);

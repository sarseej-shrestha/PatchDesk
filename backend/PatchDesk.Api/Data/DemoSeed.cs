using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.Data;

public static class DemoSeed
{
    public static async Task LoadAsync(PatchDeskDbContext db)
    {
        if (await db.Developers.AnyAsync()) return;
        var epoch = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        string[] names = ["Maya Chen", "Theo Brooks", "Aria Patel", "Noah Rivera", "Lena Park"];
        db.Developers.AddRange(names.Select((name, i) => new Developer {
            Id = i + 1, Name = name, Email = $"demo{i + 1}@example.test", Role = i == 4 ? "Reviewer" : "Engineer", CreatedAt = epoch
        }));
        string[] titles = ["Search loses filters after navigation", "Comment draft resets on refresh", "Priority sort uses alphabetical order", "Empty assignee breaks issue export", "Discount calculation fails at boundary values", "Date range excludes final day", "Long titles overflow the issue table", "Reopened issues retain resolution date", "Pagination repeats the last issue", "Concurrent edits overwrite priority", "Empty comments pass validation", "Review queue includes resolved bugs", "Add saved issue views", "Support keyboard navigation", "Add team workload overview", "Export verification history", "Add release notes workspace", "Improve search empty state", "Clarify manual resolution labels", "Improve error recovery", "Add assignee filter counts", "Refresh development dependencies", "Document database reset", "Reduce duplicate query requests", "Review accessibility labels"];
        for (var n = 0; n < titles.Length; n++)
        {
            var status = n == 4 ? IssueStatus.InProgress : (IssueStatus)(n % 5);
            if (n < 12 && status == IssueStatus.Resolved) status = IssueStatus.InReview;
            var issue = new Issue {
                Id = 100 + n, Title = titles[n],
                Description = n == 4 ? "At the $100 threshold, a 10% invoice discount is skipped. Expected $90; the isolated defective fixture returns $100." : $"Fictional demonstration issue: {titles[n].ToLowerInvariant()}. Capture the observed behavior and verify the correction before closing.",
                ReproductionSteps = n == 4 ? "1. Use subtotal 100.00.\n2. Apply discount 10%.\n3. Expected: 90.00. Defective fixture: 100.00." : "1. Open the relevant workspace.\n2. Perform the described action.\n3. Compare the result with the expected behavior.",
                IssueType = n < 12 ? IssueType.Bug : n < 17 ? IssueType.Feature : n < 21 ? IssueType.Improvement : IssueType.Maintenance,
                Priority = (Priority)(n % 4), Severity = (Severity)(n % 4), Status = status,
                AssigneeId = n % 5 + 1, ReporterId = (n + 2) % 5 + 1,
                CreatedAt = epoch.AddHours(n), UpdatedAt = epoch.AddHours(n + 1),
                ResolvedAt = status == IssueStatus.Resolved ? epoch.AddHours(n + 1) : null,
                ResolutionNote = status == IssueStatus.Resolved ? "Fictional seeded non-bug resolution; not automated test evidence." : null
            };
            issue.Activities.Add(new IssueActivity { ActorId = issue.ReporterId, Action = "SeededDemo", NewValue = $"Fictional issue initialized as {status}", Timestamp = epoch.AddHours(n) });
            if (n % 3 == 0 || n == 4) issue.Comments.Add(new IssueComment { AuthorId = 5, Content = "Please document the reproduction and link execution evidence before review. (Fictional demo comment.)", CreatedAt = epoch.AddHours(n + 1) });
            db.Issues.Add(issue);
        }
        await db.SaveChangesAsync();
    }
}

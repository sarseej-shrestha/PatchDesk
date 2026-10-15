using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Models;
namespace PatchDesk.Api.Data;

public class PatchDeskDbContext(DbContextOptions<PatchDeskDbContext> options) : DbContext(options)
{
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<IssueComment> Comments => Set<IssueComment>();
    public DbSet<IssueActivity> Activities => Set<IssueActivity>();
    public DbSet<RegressionTest> RegressionTests => Set<RegressionTest>();
    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<FixReference> FixReferences => Set<FixReference>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Developer>().HasIndex(d => d.Email).IsUnique();
        b.Entity<Developer>().Property(d => d.Name).HasMaxLength(100);
        b.Entity<Issue>().Property(i => i.Title).HasMaxLength(200);
        b.Entity<Issue>().Property(i => i.Version).IsConcurrencyToken();
        b.Entity<Issue>().HasIndex(i => new { i.Status, i.Priority, i.CreatedAt });
        b.Entity<Issue>().HasOne(i => i.Assignee).WithMany().HasForeignKey(i => i.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Issue>().HasOne(i => i.Reporter).WithMany().HasForeignKey(i => i.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Issue>().HasMany(i => i.Comments).WithOne().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Issue>().HasMany(i => i.Activities).WithOne().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Issue>().HasMany(i => i.RegressionTests).WithOne().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Issue>().HasMany(i => i.FixReferences).WithOne().HasForeignKey(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<IssueComment>().HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<IssueActivity>().HasOne(c => c.Actor).WithMany().HasForeignKey(c => c.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<IssueActivity>().HasIndex(c => c.Timestamp);
        b.Entity<RegressionTest>().HasIndex(t => new { t.IssueId, t.TestIdentifier }).IsUnique();
        b.Entity<RegressionTest>().HasMany(t => t.Executions).WithOne().HasForeignKey(e => e.RegressionTestId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TestExecution>().HasIndex(e => new { e.RegressionTestId, e.ExecutedAt });
        b.Entity<Issue>().ToTable(t => {
            t.HasCheckConstraint("CK_Issue_Enums", "IssueType BETWEEN 0 AND 3 AND Priority BETWEEN 0 AND 3 AND Severity BETWEEN 0 AND 3 AND Status BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_Issue_Title", "length(trim(Title)) BETWEEN 1 AND 200");
            t.HasCheckConstraint("CK_Issue_Version", "Version > 0 AND EvidenceRevision > 0");
        });
        b.Entity<TestExecution>().ToTable(t => t.HasCheckConstraint("CK_Execution", "Result BETWEEN 1 AND 2 AND DurationMilliseconds >= 0"));
    }
}

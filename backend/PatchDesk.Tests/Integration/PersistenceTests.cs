using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
using PatchDesk.Api.Models;
using PatchDesk.Tests.Fixtures;
namespace PatchDesk.Tests.Integration;
public class PersistenceTests
{
    [Fact]
    public async Task MigrationAndSeedAreRepeatableAndDoNotInventEvidence()
    {
        await using var f = new DatabaseFixture();
        await DemoSeed.LoadAsync(f.Db);
        await DemoSeed.LoadAsync(f.Db);
        Assert.Equal(25, await f.Db.Issues.CountAsync());
        Assert.Equal(5, await f.Db.Developers.CountAsync());
        Assert.Equal(12, await f.Db.Issues.CountAsync(i => i.IssueType == IssueType.Bug));
        Assert.Empty(await f.Db.TestExecutions.ToListAsync());
        Assert.Single(await f.Db.Database.GetAppliedMigrationsAsync());
    }
    [Fact]
    public async Task ForeignKeysRejectUnknownReporter()
    {
        await using var f = new DatabaseFixture();
        f.Db.Issues.Add(new Issue { Title = "Invalid reporter", Description = "Test", ReporterId = 999 });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync());
    }
}

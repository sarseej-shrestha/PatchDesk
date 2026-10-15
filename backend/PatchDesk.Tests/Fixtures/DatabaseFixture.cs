using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
namespace PatchDesk.Tests.Fixtures;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public PatchDeskDbContext Db { get; }
    public DatabaseFixture()
    {
        connection.Open();
        Db = new PatchDeskDbContext(new DbContextOptionsBuilder<PatchDeskDbContext>().UseSqlite(connection).Options);
        Db.Database.Migrate();
    }
    public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
}

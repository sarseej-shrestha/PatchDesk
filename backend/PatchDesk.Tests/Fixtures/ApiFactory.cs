using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
namespace PatchDesk.Tests.Fixtures;
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"patchdesk-test-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing").UseSetting("Logging:LogLevel:Default", "Warning").UseSetting("ConnectionStrings:PatchDesk", $"Data Source={path};Pooling=False");
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchDeskDbContext>();
        db.Database.Migrate();
        DemoSeed.LoadAsync(db).GetAwaiter().GetResult();
        return host;
    }
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        foreach (var suffix in new[] { "", "-shm", "-wal" }) File.Delete(path + suffix);
    }
}

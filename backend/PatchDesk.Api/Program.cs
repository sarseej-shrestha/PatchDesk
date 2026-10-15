using Microsoft.EntityFrameworkCore;
using PatchDesk.Api.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)); o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PatchDesk.Api.Middleware.ApiExceptionHandler>();
builder.Services.AddScoped<PatchDesk.Api.Services.IssueService>();
builder.Services.AddScoped<PatchDesk.Api.Services.WorkflowService>();
builder.Services.AddScoped<PatchDesk.Api.Services.RegressionService>();
builder.Services.AddSingleton<PatchDesk.Api.Services.IBuildIdentity, PatchDesk.Api.Services.BuildIdentity>();
builder.Services.AddDbContext<PatchDeskDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("PatchDesk") ?? "Data Source=patchdesk.db"));
builder.Services.AddScoped<PatchDesk.Api.Services.GitHubReferenceService>();
builder.Services.AddHttpClient<PatchDesk.Api.Services.GitHubClient>(http => {
    http.BaseAddress = new Uri("https://api.github.com/repos/sarseej-shrestha/PatchDesk/");
    http.DefaultRequestHeaders.UserAgent.ParseAdd("PatchDesk/1.0");
    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    http.Timeout = TimeSpan.FromSeconds(10);
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrWhiteSpace(token)) http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
});
var app = builder.Build();
if (args.Contains("--migrate") || args.Contains("--seed") || args.Contains("--reset"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PatchDeskDbContext>();
    if (args.Contains("--reset")) await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    if (args.Contains("--seed")) await DemoSeed.LoadAsync(db);
    return;
}
app.UseExceptionHandler();
app.MapOpenApi();
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();
app.Run();
public partial class Program;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using PatchDesk.Api.Services;
using PatchDesk.Tests.Fixtures;
namespace PatchDesk.Tests.Integration;

public class LifecycleTests
{
    // Synthetic build identity is scoped to this test server, never exported as application evidence.
    private sealed class TestBuild(bool clean = true) : IBuildIdentity
    {
        public string Commit => new('a', 40);
        public string AssemblySha256 => "isolated-test-double";
        public bool IsClean => clean;
    }
    [Fact]
    public async Task VerifiedResolutionRequiresEveryTestThenReopenInvalidatesEvidence()
    {
        await using var root = new ApiFactory();
        await using var f = root.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IBuildIdentity>(new TestBuild())));
        using var c = f.CreateClient();
        JsonElement issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104");
        int Version() => issue.GetProperty("version").GetInt32();
        async Task Mutate(string suffix, object body, bool patch = false)
        {
            var r = patch ? await c.PatchAsJsonAsync("/api/issues/104/" + suffix, body) : await c.PostAsJsonAsync("/api/issues/104/" + suffix, body);
            r.EnsureSuccessStatusCode(); issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104");
        }
        await Mutate("fix-references", new { commitHash = new string('a', 40), description = "Test fixture reference", actorId = 1, version = Version() });
        await Mutate("status", new { status = "InReview", actorId = 1, version = Version() }, true);
        var eventsBefore = issue.GetProperty("activities").GetArrayLength();
        var rejected = await c.PatchAsJsonAsync("/api/issues/104/status", new { status = "Resolved", resolutionNote = "Attempt without evidence", actorId = 5, version = Version() });
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104"); Assert.Equal(eventsBefore, issue.GetProperty("activities").GetArrayLength());
        var tests = issue.GetProperty("regressionTests").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToArray();
        await Mutate($"regression-tests/{tests[0]}/executions", new { actorId = 1, version = Version(), defectiveFixture = false });
        Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync("/api/issues/104/status", new { status = "Resolved", resolutionNote = "Only one test passed", actorId = 5, version = Version() })).StatusCode);
        foreach (var test in tests.Skip(1)) await Mutate($"regression-tests/{test}/executions", new { actorId = 1, version = Version(), defectiveFixture = false });
        await Mutate("status", new { status = "Resolved", resolutionNote = "All assertions pass for the linked build", actorId = 5, version = Version() }, true);
        Assert.Equal("Resolved", issue.GetProperty("status").GetString()); Assert.False(issue.GetProperty("resolutionOverridden").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, issue.GetProperty("resolvedAt").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/issues/104?actorId=5&version={Version()}")).StatusCode);
        await Mutate("status", new { status = "Reopened", actorId = 1, version = Version() }, true);
        Assert.Equal(JsonValueKind.Null, issue.GetProperty("resolvedAt").ValueKind);
        var transitions = await c.GetFromJsonAsync<JsonElement>("/api/issues/104/transitions"); Assert.False(transitions.GetProperty("hasCurrentEvidence").GetBoolean());
        Assert.Contains(issue.GetProperty("activities").EnumerateArray(), a => a.GetProperty("newValue").GetString() == "Reopened");
    }
    [Fact]
    public async Task DirtyBuildExecutionCannotClaimACommit()
    {
        await using var root = new ApiFactory();
        await using var f = root.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IBuildIdentity>(new TestBuild(false))));
        using var c = f.CreateClient(); var issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104");
        var test = issue.GetProperty("regressionTests")[0].GetProperty("id").GetInt32();
        var response = await c.PostAsJsonAsync($"/api/issues/104/regression-tests/{test}/executions", new { actorId = 1, version = 1 }); response.EnsureSuccessStatusCode();
        issue = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("", issue.GetProperty("regressionTests")[0].GetProperty("executions")[0].GetProperty("commitHash").GetString());
    }
    [Fact]
    public async Task ManualOverrideIsExplicitAndCannotBeUsedByAnEngineer()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        (await c.PatchAsJsonAsync("/api/issues/104/status", new { status = "InReview", actorId = 1, version = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync("/api/issues/104/status", new { status = "Resolved", resolutionNote = "Manual inspection", manualOverride = true, overrideReason = "No registry test", actorId = 1, version = 2 })).StatusCode);
        var response = await c.PatchAsJsonAsync("/api/issues/104/status", new { status = "Resolved", resolutionNote = "Manual inspection", manualOverride = true, overrideReason = "Prototype exception", actorId = 5, version = 2 }); response.EnsureSuccessStatusCode();
        var issue = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(issue.GetProperty("resolutionOverridden").GetBoolean());
        Assert.Contains(issue.GetProperty("activities").EnumerateArray(), a => a.GetProperty("action").GetString() == "ResolvedWithManualOverride" && a.GetProperty("newValue").GetString()!.Contains("Prototype exception"));
    }
    [Fact]
    public async Task ConcurrentAssignmentsHaveExactlyOneWinner()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var responses = await Task.WhenAll(c.PatchAsJsonAsync("/api/issues/104/assignee", new { assigneeId = 1, actorId = 2, version = 1 }), c.PatchAsJsonAsync("/api/issues/104/assignee", new { assigneeId = 3, actorId = 2, version = 1 }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104"); Assert.Equal(2, issue.GetProperty("version").GetInt32());
        Assert.Single(issue.GetProperty("activities").EnumerateArray(), a => a.GetProperty("action").GetString() == "Assigned");
    }
    [Theory]
    [InlineData("{\"title\":\"Defect\",\"description\":\"Details\",\"reporterId\":1,\"priority\":999}")]
    [InlineData("{\"title\":\"Defect\",\"description\":\"Details\",\"reporterId\":999}")]
    [InlineData("{\"title\":\"Defect\",\"description\":\"Details\",\"reporterId\":1,\"status\":\"Resolved\"}")]
    public async Task InvalidEnumsReportersAndOverpostingAreRejected(string body)
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var r = await c.PostAsync("/api/issues", new StringContent(body, System.Text.Encoding.UTF8, "application/json")); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
    [Fact]
    public async Task CallerCannotSupplyAPassingResult()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104"); var id = issue.GetProperty("regressionTests")[0].GetProperty("id").GetInt32();
        var r = await c.PostAsJsonAsync($"/api/issues/104/regression-tests/{id}/executions", new { actorId = 1, version = 1, result = "Passed", commitHash = new string('a', 40) }); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104"); Assert.Empty(issue.GetProperty("regressionTests")[0].GetProperty("executions").EnumerateArray());
    }
}

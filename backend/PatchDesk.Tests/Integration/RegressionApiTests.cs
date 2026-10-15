using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PatchDesk.Tests.Fixtures;
namespace PatchDesk.Tests.Integration;

public class RegressionApiTests
{
    [Fact]
    public async Task ExecutionsRecordActualBeforeAndAfterResultsAndRejectUnknownRegistryEntries()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104");
        var testId = issue.GetProperty("regressionTests")[0].GetProperty("id").GetInt32();
        var path = $"/api/issues/104/regression-tests/{testId}/executions";
        var r = await c.PostAsJsonAsync(path, new { actorId = 1, version = 1, defectiveFixture = true }); r.EnsureSuccessStatusCode();
        r = await c.PostAsJsonAsync(path, new { actorId = 1, version = 2, defectiveFixture = false }); r.EnsureSuccessStatusCode();
        issue = await c.GetFromJsonAsync<JsonElement>("/api/issues/104");
        var executions = issue.GetProperty("regressionTests")[0].GetProperty("executions");
        Assert.Equal(2, executions.GetArrayLength()); Assert.Equal("Passed", executions[0].GetProperty("result").GetString()); Assert.Equal("Failed", executions[1].GetProperty("result").GetString());
        Assert.Contains("assembly-sha256=", executions[0].GetProperty("evidence").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/issues/104/regression-tests", new { testIdentifier = "unknown", actorId = 1, version = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/issues/104/regression-tests", new { testIdentifier = "discount.boundary", actorId = 1, version = 3 })).StatusCode);
    }
}

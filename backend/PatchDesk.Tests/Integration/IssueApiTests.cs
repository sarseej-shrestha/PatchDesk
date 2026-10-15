using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PatchDesk.Tests.Fixtures;
namespace PatchDesk.Tests.Integration;

public class IssueApiTests
{
    public static object Report(string title = "New defect") => new { title, description = "Observed failure", reproductionSteps = "Run the boundary case", issueType = "Bug", priority = "High", severity = "Major", reporterId = 1, assigneeId = 2 };
    [Fact]
    public async Task CreateUpdateAssignCommentAndDeletePersist()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var response = await c.PostAsJsonAsync("/api/issues", Report()); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var issue = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = issue.GetProperty("id").GetInt32();
        var url = $"/api/issues/{id}";
        response = await c.PutAsJsonAsync(url, new { title = "Updated defect", description = "Updated", reproductionSteps = "Steps", priority = "Critical", severity = "Major", actorId = 1, version = 1 }); response.EnsureSuccessStatusCode();
        response = await c.PatchAsJsonAsync(url + "/assignee", new { assigneeId = 3, actorId = 1, version = 2 }); response.EnsureSuccessStatusCode();
        response = await c.PostAsJsonAsync(url + "/comments", new { content = "Reproduced", actorId = 3, version = 3 }); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        issue = await c.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal("Critical", issue.GetProperty("priority").GetString()); Assert.Equal(3, issue.GetProperty("assigneeId").GetInt32());
        Assert.Single(issue.GetProperty("comments").EnumerateArray()); Assert.Equal(4, issue.GetProperty("activities").GetArrayLength());
        response = await c.DeleteAsync(url + "?actorId=1&version=4"); Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(url)).StatusCode);
    }
    [Theory]
    [InlineData("/api/issues?page=0", 400)]
    [InlineData("/api/issues?status=999", 400)]
    [InlineData("/api/issues?sort=unknown", 400)]
    [InlineData("/api/issues/999999", 404)]
    public async Task InvalidRequestsReturnProblemDetails(string path, int status)
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient(); var r = await c.GetAsync(path);
        Assert.Equal(status, (int)r.StatusCode); var p = await r.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(status, p.GetProperty("status").GetInt32());
    }
    [Fact]
    public async Task RequiredFieldsAndStaleVersionsAreRejected()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/issues", Report(" "))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync("/api/issues/104/assignee", new { assigneeId = 2, actorId = 1, version = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PatchAsJsonAsync("/api/issues/104/assignee", new { assigneeId = 9999, actorId = 1, version = 1 })).StatusCode);
    }
    [Fact]
    public async Task SearchFiltersAndPaginationUseDatabaseData()
    {
        await using var f = new ApiFactory(); using var c = f.CreateClient();
        var result = await c.GetFromJsonAsync<JsonElement>("/api/issues?search=discount&status=InProgress&sort=priority&pageSize=5");
        Assert.Equal(1, result.GetProperty("total").GetInt32()); Assert.Equal(104, result.GetProperty("items")[0].GetProperty("id").GetInt32());
        var stats = await c.GetFromJsonAsync<JsonElement>("/api/dashboard/summary"); Assert.Equal(25, stats.GetProperty("total").GetInt32());
    }
}

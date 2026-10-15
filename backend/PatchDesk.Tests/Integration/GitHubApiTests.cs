using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PatchDesk.Tests.Fixtures;
namespace PatchDesk.Tests.Integration;
public class GitHubApiTests
{
    [Fact]
    public async Task LinkingCodePersistsAndInvalidatesPriorEvidence()
    {
        await using var factory = new ApiFactory(); using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/issues/104/fix-references", new { branchName = "feat/regression-testing", commitHash = (string?)null, pullRequestUrl = (string?)null, description = "Controlled registry implementation", actorId = 2, version = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var issue = await client.GetFromJsonAsync<JsonElement>("/api/issues/104");
        Assert.Single(issue.GetProperty("fixReferences").EnumerateArray()); Assert.Equal(2, issue.GetProperty("evidenceRevision").GetInt32());
        Assert.Contains(issue.GetProperty("activities").EnumerateArray(), a => a.GetProperty("action").GetString() == "FixLinked");
    }
}

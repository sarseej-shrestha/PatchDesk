using System.Net;
using PatchDesk.Api.Middleware;
using PatchDesk.Api.Services;
namespace PatchDesk.Tests.Unit;

public class GitHubClientTests
{
    private class Handler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(new HttpResponseMessage(status));
    }
    [Theory]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.Forbidden, 502)]
    [InlineData(HttpStatusCode.TooManyRequests, 502)]
    public async Task ExternalErrorsNeverBecomeSuccessfulChecks(HttpStatusCode remote, int expected)
    {
        using var http = new HttpClient(new Handler(remote)) { BaseAddress = new Uri("https://api.github.com/repos/sarseej-shrestha/PatchDesk/") };
        var error = await Assert.ThrowsAsync<ApiException>(() => new GitHubClient(http).PullRequest(1, default)); Assert.Equal(expected, error.Status);
    }
}

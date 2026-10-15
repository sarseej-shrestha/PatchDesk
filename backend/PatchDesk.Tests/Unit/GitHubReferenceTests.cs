using PatchDesk.Api.DTOs;
using PatchDesk.Api.Middleware;
using PatchDesk.Api.Services;
namespace PatchDesk.Tests.Unit;
public class GitHubReferenceTests
{
    [Theory]
    [InlineData("../main", null, null)]
    [InlineData("feature//broken", null, null)]
    [InlineData(null, "fake", null)]
    [InlineData(null, null, "javascript:alert(1)")]
    [InlineData(null, null, "https://github.com/other/repo/pull/1")]
    [InlineData(null, null, "https://github.com/sarseej-shrestha/PatchDesk/pull/1?bad=1")]
    [InlineData(null, null, null)]
    public void InvalidReferencesAreRejected(string? branch, string? commit, string? pr) => Assert.Throws<ApiException>(() => GitHubReferenceService.Validate(new FixRequest(branch, commit, pr, "Fix", 1, 1)));
    [Fact]
    public void RepositoryReferencesAreAccepted() => GitHubReferenceService.Validate(new FixRequest("fix/discount-boundary", new string('a', 40), "https://github.com/sarseej-shrestha/PatchDesk/pull/1", "Fix", 1, 1));
}

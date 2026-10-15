using Microsoft.AspNetCore.Mvc;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Services;
namespace PatchDesk.Api.Controllers;
[ApiController, Route("api")]
public class GitHubController(GitHubReferenceService references, GitHubClient github) : ControllerBase
{
    [HttpPost("issues/{id:int}/fix-references")]
    public async Task<ActionResult<IssueDto>> Add(int id, FixRequest r, CancellationToken ct)
    { var result = await references.Add(id, r, ct); return Created($"/api/issues/{id}/fix-references", result); }
    [HttpGet("github/pulls/{number:int}")]
    public Task<PullRequestState> PullRequest(int number, CancellationToken ct) => github.PullRequest(number, ct);
}

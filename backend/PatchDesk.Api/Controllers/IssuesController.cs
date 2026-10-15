using Microsoft.AspNetCore.Mvc;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Services;
namespace PatchDesk.Api.Controllers;

[ApiController, Route("api/issues")]
public class IssuesController(IssueService issues) : ControllerBase
{
    [HttpGet] public Task<PageDto<IssueDto>> List([FromQuery] IssueQuery query, CancellationToken ct) => issues.List(query, ct);
    [HttpGet("{id:int}")] public async Task<IssueDto> Get(int id, CancellationToken ct) => (await issues.Get(id, ct)).ToDto();
    [HttpPost]
    public async Task<ActionResult<IssueDto>> Create(CreateIssueRequest r, CancellationToken ct)
    {
        var issue = await issues.Create(r, ct); return CreatedAtAction(nameof(Get), new { id = issue.Id }, issue);
    }
    [HttpPut("{id:int}")] public Task<IssueDto> Update(int id, UpdateIssueRequest r, CancellationToken ct) => issues.Update(id, r, ct);
    [HttpPatch("{id:int}/assignee")] public Task<IssueDto> Assign(int id, AssignRequest r, CancellationToken ct) => issues.Assign(id, r, ct);
    [HttpGet("{id:int}/comments")] public async Task<IReadOnlyList<CommentDto>> Comments(int id, CancellationToken ct) => (await issues.Get(id, ct)).ToDto().Comments;
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentDto>> Comment(int id, CommentRequest r, CancellationToken ct)
    {
        var comment = await issues.Comment(id, r, ct); return Created($"/api/issues/{id}/comments", comment);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int actorId, [FromQuery] int version, CancellationToken ct)
    { await issues.Delete(id, actorId, version, ct); return NoContent(); }
}

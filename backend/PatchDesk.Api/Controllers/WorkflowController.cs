using Microsoft.AspNetCore.Mvc;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Services;
namespace PatchDesk.Api.Controllers;

[ApiController, Route("api/issues/{id:int}")]
public class WorkflowController(WorkflowService workflow, IssueService issues) : ControllerBase
{
    [HttpPatch("status")] public Task<IssueDto> Transition(int id, TransitionRequest r, CancellationToken ct) => workflow.Transition(id, r, ct);
    [HttpGet("transitions")]
    public async Task<object> Transitions(int id, CancellationToken ct)
    {
        var i = await issues.Get(id, ct);
        return new { allowed = WorkflowEngine.Allowed(i.Status), hasCurrentEvidence = WorkflowEngine.HasCurrentEvidence(i) };
    }
}

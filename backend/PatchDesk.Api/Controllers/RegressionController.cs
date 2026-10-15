using Microsoft.AspNetCore.Mvc;
using PatchDesk.Api.DTOs;
using PatchDesk.Api.Regression;
using PatchDesk.Api.Services;
namespace PatchDesk.Api.Controllers;
[ApiController, Route("api")]
public class RegressionController(RegressionService regression, IBuildIdentity build) : ControllerBase
{
    [HttpGet("regression-registry")] public object Registry() => new { tests = RegressionRegistry.Tests, buildCommit = build.Commit, build.IsClean, build.AssemblySha256, executionSource = "PatchDesk controlled registry" };
    [HttpPost("issues/{id:int}/regression-tests")] public async Task<ActionResult<IssueDto>> Associate(int id, AssociateTestRequest r, CancellationToken ct)
    { var result = await regression.Associate(id, r, ct); return Created($"/api/issues/{id}/regression-tests", result); }
    [HttpPost("issues/{id:int}/regression-tests/{testId:int}/executions")]
    public Task<IssueDto> Execute(int id, int testId, ExecuteTestRequest r, CancellationToken ct) => regression.Execute(id, testId, r, ct);
}

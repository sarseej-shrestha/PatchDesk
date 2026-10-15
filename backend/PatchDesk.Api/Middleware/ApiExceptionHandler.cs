using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace PatchDesk.Api.Middleware;

public class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception ex, CancellationToken ct)
    {
        var (status, detail) = ex switch {
            ApiException e => (e.Status, e.Message),
            DbUpdateConcurrencyException => (409, "This issue changed. Refresh it and retry with the current version."),
            _ => (500, "An unexpected error occurred. See the server log using the trace identifier.")
        };
        if (status == 500) logger.LogError(ex, "Request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails {
            Status = status, Title = status == 409 ? "Conflict" : status == 404 ? "Not found" : status == 400 ? "Invalid request" : "Server error",
            Detail = detail, Instance = context.Request.Path, Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken: ct);
        return true;
    }
}

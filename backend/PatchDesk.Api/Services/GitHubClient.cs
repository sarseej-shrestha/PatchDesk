using System.Net;
using System.Text.Json;
using PatchDesk.Api.Middleware;
namespace PatchDesk.Api.Services;

public record GitHubCheck(string Name, string Status, string? Conclusion, string? Url);
public record PullRequestState(int Number, string Title, string State, bool Merged, string Url, string HeadCommit, IReadOnlyList<GitHubCheck> Checks, DateTime FetchedAt);
public class GitHubClient(HttpClient http)
{
    private async Task<JsonDocument> Get(string path, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) throw new ApiException(404, "GitHub reference not found in PatchDesk.");
            if (!response.IsSuccessStatusCode) throw new ApiException(502, $"GitHub returned {(int)response.StatusCode}. No check status is available; it has not been inferred.");
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        }
        catch (HttpRequestException) { throw new ApiException(502, "GitHub is unavailable. Stored references remain accessible."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new ApiException(504, "GitHub request timed out."); }
    }
    public async Task<PullRequestState> PullRequest(int number, CancellationToken ct)
    {
        if (number < 1) throw new ApiException(400, "Pull request number must be positive.");
        using var pr = await Get($"pulls/{number}", ct);
        var p = pr.RootElement; var head = p.GetProperty("head").GetProperty("sha").GetString()!;
        using var checks = await Get($"commits/{Uri.EscapeDataString(head)}/check-runs?per_page=100", ct);
        var rows = checks.RootElement.GetProperty("check_runs").EnumerateArray().Select(c => new GitHubCheck(c.GetProperty("name").GetString()!, c.GetProperty("status").GetString()!, c.GetProperty("conclusion").GetString(), c.GetProperty("html_url").GetString())).ToArray();
        return new(number, p.GetProperty("title").GetString()!, p.GetProperty("state").GetString()!, p.GetProperty("merged").GetBoolean(), p.GetProperty("html_url").GetString()!, head, rows, DateTime.UtcNow);
    }
}

using System.Net;
using System.Text.RegularExpressions;
using CoreWorkplaceType = JobSearchTracker.Core.Enums.WorkplaceType;

namespace JobSearchTracker.Data.Services;

public class LinkedInJobDetails
{
    public CoreWorkplaceType? WorkplaceType { get; set; }
    public string? Salary { get; set; }
}

/// <summary>
/// Fetches a single LinkedIn job posting's full page using the user's own session cookie, to
/// read fields (workplace type, salary) that LinkedIn only shows to logged-in viewers and
/// omits from the public jobs-guest endpoint.
///
/// Intentionally on-demand and single-posting only: call this per posting, never in bulk. Unlike
/// LinkedInJobSearchService this uses an authenticated session, so keeping volume low here matters
/// even more than for the public search.
/// </summary>
public class LinkedInAuthenticatedDetailService(HttpClient httpClient, AppSettingsService settings)
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public async Task<LinkedInJobDetails> FetchDetailsAsync(string postingUrl, CancellationToken ct = default)
    {
        var cookie = await settings.GetAsync(AppSettingsService.LinkedInSessionCookieKey);
        if (string.IsNullOrWhiteSpace(cookie))
            throw new InvalidOperationException("No LinkedIn session cookie configured. Add one on the Settings page first.");

        var html = await FetchWithBackoffAsync(postingUrl, cookie, ct);
        return ParseDetails(html);
    }

    private async Task<string> FetchWithBackoffAsync(string url, string cookie, CancellationToken ct)
    {
        const int maxRetries = 4;
        var delayMs = 500;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Add("Cookie", $"li_at={cookie}");

            using var response = await httpClient.SendAsync(request, ct);

            if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
            {
                if (attempt == maxRetries)
                    throw new InvalidOperationException($"LinkedIn request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

                var jitter = Random.Shared.Next(0, 500);
                await Task.Delay(delayMs + jitter, ct);
                delayMs = Math.Min(delayMs * 2, 8000);
                continue;
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new InvalidOperationException(
                    "LinkedIn rejected the request — your session cookie may have expired. Update it on the Settings page.");

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"LinkedIn request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

            return await response.Content.ReadAsStringAsync(ct);
        }

        throw new InvalidOperationException("LinkedIn request failed after max retries");
    }

    /// <summary>
    /// Best-effort: scoped to the first part of the page (the job's own header/description area)
    /// to avoid accidentally matching a different job's workplace type in a "similar jobs" section
    /// further down. LinkedIn's markup for logged-in pages isn't documented, so this may need
    /// adjustment once tried against a real session.
    /// </summary>
    private static LinkedInJobDetails ParseDetails(string html)
    {
        var scoped = html.Length > 30000 ? html[..30000] : html;

        CoreWorkplaceType? workplaceType = null;
        var wtMatch = Regex.Match(scoped, @"\b(On-site|Remote|Hybrid)\b", RegexOptions.IgnoreCase);
        if (wtMatch.Success)
        {
            workplaceType = wtMatch.Value.ToLowerInvariant() switch
            {
                "on-site" => CoreWorkplaceType.OnSite,
                "remote" => CoreWorkplaceType.Remote,
                "hybrid" => CoreWorkplaceType.Hybrid,
                _ => null
            };
        }

        string? salary = null;
        var salaryMatch = Regex.Match(
            scoped,
            @"\$[\d,]+(?:\.\d+)?(?:\s*/\s*(?:yr|hr|year|hour))?\s*(?:-|–|to)\s*\$[\d,]+(?:\.\d+)?(?:\s*/\s*(?:yr|hr|year|hour))?",
            RegexOptions.IgnoreCase);
        if (salaryMatch.Success) salary = salaryMatch.Value.Trim();

        return new LinkedInJobDetails { WorkplaceType = workplaceType, Salary = salary };
    }
}

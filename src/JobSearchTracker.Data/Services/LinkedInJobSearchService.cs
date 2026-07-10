using System.Net;
using System.Text.RegularExpressions;
using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Data.Services;

public class LinkedInJobSearchResult
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string PostingUrl { get; set; } = string.Empty;
    public DateOnly? PostedDate { get; set; }
}

/// <summary>
/// Fetches job listings from LinkedIn's public, unauthenticated "jobs-guest" endpoint —
/// the same one LinkedIn's own guest job-search pages use. No login or API key involved.
///
/// This is still automated access under LinkedIn's Terms of Service, so callers must keep
/// volume low: few pages per run, delay between requests, personal use only.
/// </summary>
public class LinkedInJobSearchService(HttpClient httpClient)
{
    private const string SearchUrl = "https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public async Task<List<LinkedInJobSearchResult>> SearchAsync(
        string keywords,
        string location,
        WorkplaceType? workplaceType,
        DatePostedFilter datePosted,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl(keywords, location, workplaceType, datePosted, page);
        var html = await FetchWithBackoffAsync(url, ct);
        return ParseJobCards(html);
    }

    private static string BuildUrl(
        string keywords, string location, WorkplaceType? workplaceType, DatePostedFilter datePosted, int page)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(keywords)) parts.Add($"keywords={Uri.EscapeDataString(keywords)}");
        if (!string.IsNullOrWhiteSpace(location)) parts.Add($"location={Uri.EscapeDataString(location)}");

        var tpr = DatePostedToTpr(datePosted);
        if (tpr is not null) parts.Add($"f_TPR={tpr}");

        var wt = WorkplaceTypeToFlag(workplaceType);
        if (wt is not null) parts.Add($"f_WT={wt}");

        parts.Add($"start={(page - 1) * 10}");

        return $"{SearchUrl}?{string.Join('&', parts)}";
    }

    private static string? DatePostedToTpr(DatePostedFilter filter) => filter switch
    {
        DatePostedFilter.Past24Hours => "r86400",
        DatePostedFilter.PastWeek => "r604800",
        DatePostedFilter.PastMonth => "r2592000",
        _ => null
    };

    private static string? WorkplaceTypeToFlag(WorkplaceType? type) => type switch
    {
        WorkplaceType.OnSite => "1",
        WorkplaceType.Remote => "2",
        WorkplaceType.Hybrid => "3",
        _ => null
    };

    private async Task<string> FetchWithBackoffAsync(string url, CancellationToken ct)
    {
        const int maxRetries = 6;
        var delayMs = 500;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

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

            if (response.StatusCode == HttpStatusCode.NotFound) return string.Empty;

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"LinkedIn request failed: {(int)response.StatusCode} {response.ReasonPhrase}");

            return await response.Content.ReadAsStringAsync(ct);
        }

        throw new InvalidOperationException("LinkedIn request failed after max retries");
    }

    private static string StripTags(string html)
    {
        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        return Regex.Replace(noTags, @"\s+", " ").Trim();
    }

    private static string Clean(string html) => WebUtility.HtmlDecode(StripTags(html));

    /// <summary>
    /// Parses the search response: a flat list of &lt;li&gt; job cards. Splits on the
    /// job-posting URN and parses each chunk independently so one malformed card can't
    /// break the rest.
    /// </summary>
    public static List<LinkedInJobSearchResult> ParseJobCards(string html)
    {
        var results = new List<LinkedInJobSearchResult>();
        var chunks = Regex.Split(html, "data-entity-urn=\"urn:li:jobPosting:").Skip(1);

        foreach (var chunk in chunks)
        {
            var idMatch = Regex.Match(chunk, @"^(\d+)");
            if (!idMatch.Success) continue;
            var id = idMatch.Groups[1].Value;

            var linkMatch = Regex.Match(
                chunk, "class=\"base-card__full-link[^\"]*\"[^>]*href=\"([^\"]+)\"", RegexOptions.IgnoreCase);
            var url = linkMatch.Success ? WebUtility.HtmlDecode(linkMatch.Groups[1].Value).Split('?')[0] : "";

            string? title = null;
            var h3 = Regex.Match(
                chunk, "class=\"base-search-card__title\"[^>]*>([\\s\\S]*?)</h3>", RegexOptions.IgnoreCase);
            if (h3.Success) title = Clean(h3.Groups[1].Value);
            if (string.IsNullOrEmpty(title))
            {
                var sr = Regex.Match(chunk, "class=\"sr-only\"[^>]*>([\\s\\S]*?)</span>", RegexOptions.IgnoreCase);
                if (sr.Success) title = Clean(sr.Groups[1].Value);
            }
            if (string.IsNullOrEmpty(title)) continue;

            string? company = null;
            var sub = Regex.Match(
                chunk, "class=\"base-search-card__subtitle\"[^>]*>([\\s\\S]*?)</h4>", RegexOptions.IgnoreCase);
            if (sub.Success)
            {
                company = Clean(sub.Groups[1].Value);
                if (string.IsNullOrEmpty(company)) company = null;
            }

            var loc = Regex.Match(
                chunk, "class=\"job-search-card__location\"[^>]*>([\\s\\S]*?)</span>", RegexOptions.IgnoreCase);
            var location = loc.Success ? Clean(loc.Groups[1].Value) : null;
            if (string.IsNullOrEmpty(location)) location = null;

            var dt = Regex.Match(
                chunk, "class=\"job-search-card__listdate[^\"]*\"[^>]*datetime=\"([^\"]+)\"", RegexOptions.IgnoreCase);
            DateOnly? postedDate = null;
            if (dt.Success && DateOnly.TryParse(dt.Groups[1].Value, out var parsed)) postedDate = parsed;

            results.Add(new LinkedInJobSearchResult
            {
                ExternalId = id,
                Title = title,
                CompanyName = company,
                Location = location,
                PostingUrl = string.IsNullOrEmpty(url) ? $"https://www.linkedin.com/jobs/view/{id}" : url,
                PostedDate = postedDate
            });
        }

        return results;
    }
}

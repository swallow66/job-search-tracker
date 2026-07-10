using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class JobSearchRunResult
{
    public int SearchRequestId { get; set; }
    public int NewPostingsCount { get; set; }
}

/// <summary>
/// Runs saved searches against LinkedIn's public jobs-guest endpoint and saves new results.
/// Deliberately capped and paced to keep request volume low (see LinkedInJobSearchService).
/// </summary>
public class JobSearchRunnerService(AppDbContext db, LinkedInJobSearchService linkedIn)
{
    private const int PagesPerRun = 2; // up to 20 results per run
    private static readonly TimeSpan DelayBetweenPages = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DelayBetweenSearches = TimeSpan.FromSeconds(3);

    public async Task<int> RunAsync(int searchRequestId, CancellationToken ct = default)
    {
        var request = await db.SearchRequests.FindAsync([searchRequestId], ct)
            ?? throw new InvalidOperationException("Search request not found.");

        var existingIds = (await db.JobPostings
            .Where(p => p.SearchRequestId == searchRequestId)
            .Select(p => p.ExternalId)
            .ToListAsync(ct)).ToHashSet();

        var newCount = 0;
        for (var page = 1; page <= PagesPerRun; page++)
        {
            var results = await linkedIn.SearchAsync(
                request.Keywords, request.Location, request.WorkplaceType, request.DatePosted, page, ct);

            if (results.Count == 0) break;

            foreach (var result in results)
            {
                if (!existingIds.Add(result.ExternalId)) continue;

                db.JobPostings.Add(new JobPosting
                {
                    SearchRequestId = searchRequestId,
                    ExternalId = result.ExternalId,
                    Title = result.Title,
                    CompanyName = result.CompanyName,
                    Location = result.Location,
                    PostingUrl = result.PostingUrl,
                    PostedDate = result.PostedDate
                });
                newCount++;
            }

            if (results.Count < 10) break; // last page of results
            if (page < PagesPerRun) await Task.Delay(DelayBetweenPages, ct);
        }

        request.LastRunAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return newCount;
    }

    public async Task<List<JobSearchRunResult>> RunAllActiveAsync(CancellationToken ct = default)
    {
        var activeIds = await db.SearchRequests
            .Where(s => s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var results = new List<JobSearchRunResult>();
        for (var i = 0; i < activeIds.Count; i++)
        {
            var count = await RunAsync(activeIds[i], ct);
            results.Add(new JobSearchRunResult { SearchRequestId = activeIds[i], NewPostingsCount = count });
            if (i < activeIds.Count - 1) await Task.Delay(DelayBetweenSearches, ct);
        }

        return results;
    }
}

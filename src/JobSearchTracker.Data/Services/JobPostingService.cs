using JobSearchTracker.Core.Entities;
using JobSearchTracker.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class JobPostingService(AppDbContext db)
{
    public async Task<List<JobPosting>> GetBySearchRequestAsync(int searchRequestId) =>
        await db.JobPostings
            .Where(p => p.SearchRequestId == searchRequestId)
            .OrderByDescending(p => p.PostedDate)
            .ThenByDescending(p => p.DiscoveredAt)
            .ToListAsync();

    public async Task<JobPosting?> GetByIdAsync(int id) =>
        await db.JobPostings.Include(p => p.SearchRequest).FirstOrDefaultAsync(p => p.Id == id);

    public async Task MarkViewedAsync(int id)
    {
        var posting = await db.JobPostings.FindAsync(id);
        if (posting is not null && posting.Status == JobPostingStatus.New)
        {
            posting.Status = JobPostingStatus.Viewed;
            posting.ViewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAppliedAsync(int id, int applicationId)
    {
        var posting = await db.JobPostings.FindAsync(id);
        if (posting is not null)
        {
            posting.Status = JobPostingStatus.Applied;
            posting.AppliedAt = DateTime.UtcNow;
            posting.ApplicationId = applicationId;
            await db.SaveChangesAsync();
        }
    }

    public async Task DismissAsync(int id)
    {
        var posting = await db.JobPostings.FindAsync(id);
        if (posting is not null)
        {
            posting.Status = JobPostingStatus.Dismissed;
            await db.SaveChangesAsync();
        }
    }
}

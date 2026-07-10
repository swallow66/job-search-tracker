using JobSearchTracker.Core.Entities;
using JobSearchTracker.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class SearchRequestService(AppDbContext db)
{
    public async Task<List<SearchRequest>> GetAllAsync() =>
        await db.SearchRequests.OrderByDescending(s => s.CreatedAt).ToListAsync();

    public async Task<SearchRequest?> GetByIdAsync(int id) =>
        await db.SearchRequests.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Dictionary<int, int>> GetNewCountsAsync() =>
        await db.JobPostings
            .Where(p => p.Status == JobPostingStatus.New)
            .GroupBy(p => p.SearchRequestId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

    public async Task<SearchRequest> CreateAsync(SearchRequest request)
    {
        request.CreatedAt = DateTime.UtcNow;
        db.SearchRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    public async Task UpdateAsync(SearchRequest request)
    {
        db.SearchRequests.Update(request);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var request = await db.SearchRequests.FindAsync(id);
        if (request is not null)
        {
            db.SearchRequests.Remove(request);
            await db.SaveChangesAsync();
        }
    }
}

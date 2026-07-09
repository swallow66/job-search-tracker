using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class ApplicationService(AppDbContext db)
{
    public async Task<List<Application>> GetAllAsync(ApplicationFilter? filter = null)
    {
        var query = db.Applications.Include(a => a.Company).AsQueryable();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.CompanyNameContains))
                query = query.Where(a => a.Company.Name.Contains(filter.CompanyNameContains));
            if (filter.Source is not null)
                query = query.Where(a => a.Source == filter.Source);
            if (filter.FromDate is not null)
                query = query.Where(a => a.ApplicationDate >= filter.FromDate);
            if (filter.ToDate is not null)
                query = query.Where(a => a.ApplicationDate <= filter.ToDate);
        }

        return await query.OrderByDescending(a => a.ApplicationDate).ToListAsync();
    }

    public async Task<Application?> GetByIdAsync(int id) =>
        await db.Applications.Include(a => a.Company).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Application> CreateAsync(Application application)
    {
        application.CreatedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;
        db.Applications.Add(application);
        await db.SaveChangesAsync();
        return application;
    }

    public async Task UpdateAsync(Application application)
    {
        application.UpdatedAt = DateTime.UtcNow;
        db.Applications.Update(application);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var application = await db.Applications.FindAsync(id);
        if (application is not null)
        {
            db.Applications.Remove(application);
            await db.SaveChangesAsync();
        }
    }
}

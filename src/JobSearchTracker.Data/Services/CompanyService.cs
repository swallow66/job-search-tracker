using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class CompanyService(AppDbContext db)
{
    public async Task<List<Company>> GetAllAsync() =>
        await db.Companies.OrderBy(c => c.Name).ToListAsync();

    public async Task<Company> GetOrCreateByNameAsync(string name)
    {
        var trimmedName = name.Trim();
        var existing = await db.Companies.FirstOrDefaultAsync(c => c.Name == trimmedName);
        if (existing is not null)
            return existing;

        var company = new Company { Name = trimmedName };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }
}

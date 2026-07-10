using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data.Services;

public class AppSettingsService(AppDbContext db)
{
    public const string LinkedInSessionCookieKey = "LinkedIn:SessionCookie";

    public async Task<string?> GetAsync(string key) =>
        (await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key))?.Value;

    public async Task SetAsync(string key, string value)
    {
        var existing = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing is null)
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            existing.Value = value;

        await db.SaveChangesAsync();
    }
}

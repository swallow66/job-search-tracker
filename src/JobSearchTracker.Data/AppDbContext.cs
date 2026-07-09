using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationContact> ApplicationContacts => Set<ApplicationContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<Contact>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.HasOne(c => c.Company)
                .WithMany(co => co.Contacts)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Application>(e =>
        {
            e.HasOne(a => a.Company)
                .WithMany(c => c.Applications)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.ApplicationDate);
        });

        modelBuilder.Entity<ApplicationContact>(e =>
        {
            e.HasKey(ac => new { ac.ApplicationId, ac.ContactId });
            e.HasOne(ac => ac.Application)
                .WithMany(a => a.ApplicationContacts)
                .HasForeignKey(ac => ac.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ac => ac.Contact)
                .WithMany(c => c.ApplicationContacts)
                .HasForeignKey(ac => ac.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

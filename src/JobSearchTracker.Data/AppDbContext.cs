using JobSearchTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobSearchTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApplicationContact> ApplicationContacts => Set<ApplicationContact>();
    public DbSet<SearchRequest> SearchRequests => Set<SearchRequest>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

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

        modelBuilder.Entity<SearchRequest>(e =>
        {
            e.Property(s => s.Name).IsRequired().HasMaxLength(200);
            e.Property(s => s.Keywords).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<JobPosting>(e =>
        {
            e.Property(p => p.ExternalId).IsRequired().HasMaxLength(50);
            e.Property(p => p.Title).IsRequired().HasMaxLength(300);
            e.Property(p => p.PostingUrl).IsRequired().HasMaxLength(1000);
            e.HasIndex(p => new { p.SearchRequestId, p.ExternalId }).IsUnique();
            e.HasOne(p => p.SearchRequest)
                .WithMany(s => s.JobPostings)
                .HasForeignKey(p => p.SearchRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Application)
                .WithMany()
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.Property(s => s.Key).IsRequired().HasMaxLength(100);
            e.HasIndex(s => s.Key).IsUnique();
        });
    }
}

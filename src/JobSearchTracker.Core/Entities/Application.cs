using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Core.Entities;

public class Application
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public DateOnly ApplicationDate { get; set; }
    public string? PositionTitle { get; set; }
    public string? JobPostingUrl { get; set; }

    public ApplicationSource Source { get; set; }
    public string? SourceOtherDescription { get; set; }
    public string? SourceUrl { get; set; }

    public string? ResumeVersion { get; set; }
    public string? ResumeFilePath { get; set; }

    /// <summary>JSON bag for future fields (e.g. scraped posting metadata) without a schema migration per feature.</summary>
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ApplicationContact> ApplicationContacts { get; set; } = [];
}

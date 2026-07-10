using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Core.Entities;

public class JobPosting
{
    public int Id { get; set; }

    public int SearchRequestId { get; set; }
    public SearchRequest SearchRequest { get; set; } = null!;

    /// <summary>LinkedIn's numeric job ID, used to de-duplicate results across search runs.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string PostingUrl { get; set; } = string.Empty;
    public DateOnly? PostedDate { get; set; }

    public JobPostingStatus Status { get; set; } = JobPostingStatus.New;
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ViewedAt { get; set; }
    public DateTime? AppliedAt { get; set; }

    public int? ApplicationId { get; set; }
    public Application? Application { get; set; }
}

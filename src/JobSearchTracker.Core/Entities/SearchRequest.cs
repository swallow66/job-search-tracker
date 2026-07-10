using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Core.Entities;

public class SearchRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public WorkplaceType? WorkplaceType { get; set; }
    public DatePostedFilter DatePosted { get; set; } = DatePostedFilter.AnyTime;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }

    public List<JobPosting> JobPostings { get; set; } = [];
}

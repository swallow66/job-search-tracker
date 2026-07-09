using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Data.Services;

public class ApplicationFilter
{
    public string? CompanyNameContains { get; set; }
    public ApplicationSource? Source { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

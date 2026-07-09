using System.ComponentModel.DataAnnotations;
using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Web.Models;

public class ApplicationFormModel
{
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Application date is required")]
    public DateOnly ApplicationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(300)]
    public string? PositionTitle { get; set; }

    [Url(ErrorMessage = "Enter a valid URL")]
    public string? JobPostingUrl { get; set; }

    public ApplicationSource Source { get; set; } = ApplicationSource.LinkedIn;

    [StringLength(200)]
    public string? SourceOtherDescription { get; set; }

    [Url(ErrorMessage = "Enter a valid URL")]
    public string? SourceUrl { get; set; }

    [StringLength(200)]
    public string? ResumeVersion { get; set; }

    [StringLength(500)]
    public string? ResumeFilePath { get; set; }
}

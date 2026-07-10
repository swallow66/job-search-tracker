using System.ComponentModel.DataAnnotations;
using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Web.Models;

public class SearchRequestFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Keywords are required")]
    [StringLength(200)]
    public string Keywords { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required")]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    public WorkplaceType? WorkplaceType { get; set; }
    public DatePostedFilter DatePosted { get; set; } = DatePostedFilter.AnyTime;
    public bool IsActive { get; set; } = true;
}

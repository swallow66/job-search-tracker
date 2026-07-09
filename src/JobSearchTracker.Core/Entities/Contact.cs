using JobSearchTracker.Core.Enums;

namespace JobSearchTracker.Core.Entities;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ContactType Type { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public List<ApplicationContact> ApplicationContacts { get; set; } = [];
}

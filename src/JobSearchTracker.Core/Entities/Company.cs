namespace JobSearchTracker.Core.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Notes { get; set; }

    public List<Application> Applications { get; set; } = [];
    public List<Contact> Contacts { get; set; } = [];
}

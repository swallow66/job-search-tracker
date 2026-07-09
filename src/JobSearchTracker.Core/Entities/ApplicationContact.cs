namespace JobSearchTracker.Core.Entities;

public class ApplicationContact
{
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
}

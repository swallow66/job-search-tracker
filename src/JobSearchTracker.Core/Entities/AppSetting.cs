namespace JobSearchTracker.Core.Entities;

/// <summary>Simple local key-value store for settings that must never be committed to git (e.g. session cookies).</summary>
public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

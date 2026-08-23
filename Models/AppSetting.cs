namespace IslamiJindegiApi.Models;

/// <summary>
/// Public, app-wide feature switches. There is intentionally one active row.
/// </summary>
public class AppSetting
{
    public Guid Id { get; set; }
    public bool AskQuestion { get; set; }
    public bool DisplayOfflineQuran { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

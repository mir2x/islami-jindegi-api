namespace IslamiJindegiApi.Models;

public class NamazTime
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleBn { get; set; }
    public string Masail { get; set; } = string.Empty;
    public string? Fazail { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

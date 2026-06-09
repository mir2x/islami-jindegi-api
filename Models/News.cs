namespace IslamiJindegiApi.Models;

public class News
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool Published { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

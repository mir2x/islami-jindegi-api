namespace IslamiJindegiApi.Models;

public class Dua
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public bool Published { get; set; } = true;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = [];
}

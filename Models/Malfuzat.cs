namespace IslamiJindegiApi.Models;

public class Malfuzat
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Excerpt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public bool HasAudio { get; set; }
    public bool Published { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public int? Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
}

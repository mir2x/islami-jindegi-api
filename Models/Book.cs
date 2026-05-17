namespace IslamiJindegiApi.Models;

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? Publisher { get; set; }
    public string? Price { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? CoverUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public int Position { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool Published { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Author> Authors { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Chapter> Chapters { get; set; } = [];
}

namespace IslamiJindegiApi.Models;

public class Bayan
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? AudioUrl { get; set; }
    public bool Published { get; set; } = true;
    public DateTime PublishedAt { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid AuthorId { get; set; }
    public Author Author { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = [];
}

namespace IslamiJindegiApi.Models;

public class Chapter
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public ICollection<SubChapter> SubChapters { get; set; } = [];
}

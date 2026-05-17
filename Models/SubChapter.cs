namespace IslamiJindegiApi.Models;

public class SubChapter
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;
}

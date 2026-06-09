namespace IslamiJindegiApi.Models;

public class Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Info { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Book> Books { get; set; } = [];
    public ICollection<Malfuzat> Malfuzats { get; set; } = [];
    public ICollection<Masail> Masails { get; set; } = [];
    public ICollection<Bayan> Bayans { get; set; } = [];
    public ICollection<Article> Articles { get; set; } = [];
}

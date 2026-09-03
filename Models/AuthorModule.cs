namespace IslamiJindegiApi.Models;

/// <summary>
/// Which modules an author belongs to, and where they sit in that module's list.
/// The old system kept five separate author tables (`authors`, `speakers`, `article_authors`,
/// `malfuzat_authors`, `masail_authors`), each with its own `position`. Unifying them into one
/// `Authors` table collapsed five orderings into a single Position column — which ended up
/// holding the books ordering — so the per-module list endpoints fell back to sorting by
/// content count instead.
/// </summary>
public class AuthorModule
{
    public Guid AuthorId { get; set; }
    public string Module { get; set; } = string.Empty; // book | bayan | malfuzat | masail | article
    public int Position { get; set; }

    public Author Author { get; set; } = null!;
}

/// <summary>
/// The modules that attribute content to an author. Deliberately not <see cref="ContentModules"/>:
/// dua has no author, so the author module list is five, not six.
/// </summary>
public static class AuthorModules
{
    public const string Book = ContentModules.Book;
    public const string Bayan = ContentModules.Bayan;
    public const string Malfuzat = ContentModules.Malfuzat;
    public const string Masail = ContentModules.Masail;
    public const string Article = ContentModules.Article;

    public static readonly string[] All = [Book, Bayan, Malfuzat, Masail, Article];
}

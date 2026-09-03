namespace IslamiJindegiApi.Models;

/// <summary>
/// Which modules a category belongs to, and where it sits in that module's list.
/// Membership used to be inferred from content counts, so a category could not be added to a
/// module before it had content, and every module shared the single Category.Position column.
/// </summary>
public class CategoryModule
{
    public Guid CategoryId { get; set; }
    public string Module { get; set; } = string.Empty; // book | bayan | malfuzat | masail | dua | article
    public int Position { get; set; }

    public Category Category { get; set; } = null!;
}

public static class ContentModules
{
    public const string Book = "book";
    public const string Bayan = "bayan";
    public const string Malfuzat = "malfuzat";
    public const string Masail = "masail";
    public const string Dua = "dua";
    public const string Article = "article";

    public static readonly string[] All = [Book, Bayan, Malfuzat, Masail, Dua, Article];
}

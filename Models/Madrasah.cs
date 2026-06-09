namespace IslamiJindegiApi.Models;

public class Madrasah
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Introduction { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<MadrasahInfo> Infos { get; set; } = [];
    public ICollection<MadrasahPhoto> Photos { get; set; } = [];
}

public class MadrasahInfo
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid MadrasahId { get; set; }
    public Madrasah Madrasah { get; set; } = null!;
}

public class MadrasahPhoto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid MadrasahId { get; set; }
    public Madrasah Madrasah { get; set; } = null!;
}

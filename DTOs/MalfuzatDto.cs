namespace IslamiJindegiApi.DTOs;

public record MalfuzatListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    bool Published,
    bool IsOfflineAvailable,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories);

public record MalfuzatDetail(
    Guid Id,
    string Title,
    string? Body,
    string? Excerpt,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    bool IsOfflineAvailable,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories,
    SiblingRef? Previous = null,
    SiblingRef? Next = null);

/// <summary>`Position` is the author's place in THIS module's list (author_modules), which is
/// what the list is already sorted by. The app stores it so its offline filter can reproduce
/// the same order without re-downloading content.</summary>
public record MalfuzatAuthorOption(Guid Id, string Name, int Count, int Position);
public record MalfuzatCategoryOption(Guid Id, string Title, int Count);

public record SaveMalfuzatRequest(
    string Title,
    string? Body,
    string? Excerpt,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
    Guid AuthorId,
    List<Guid> CategoryIds);

namespace IslamiJindegiApi.DTOs;

public record BayanListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    string? Location,
    string? AudioUrl,
    bool Published,
    bool IsOfflineAvailable,
    DateTime PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories);

public record BayanDetail(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    string? Location,
    string? AudioUrl,
    bool Published,
    bool IsOfflineAvailable,
    DateTime PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories,
    SiblingRef? Previous = null,
    SiblingRef? Next = null);

public record SaveBayanRequest(
    string Title,
    string? Excerpt,
    string Language,
    string? Location,
    string? AudioUrl,
    bool Published,
    DateTime PublishedAt,
    int? Position,
    Guid AuthorId,
    List<Guid> CategoryIds);

/// <summary>`Position` is the author's place in THIS module's list (author_modules), which is
/// what the list is already sorted by. The app stores it so its offline filter can reproduce
/// the same order without re-downloading content.</summary>
public record BayanAuthorOption(Guid Id, string Name, int Count, int Position);

public record BayanCategoryOption(Guid Id, string Title, int Count);

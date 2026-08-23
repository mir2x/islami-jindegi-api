namespace IslamiJindegiApi.DTOs;

public record BookListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string? Publisher,
    string? Price,
    string Language,
    string? CoverUrl,
    string? DocumentUrl,
    int Position,
    DateTime? PublishedAt,
    bool Published,
    bool IsOfflineAvailable,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<AuthorResponse> Authors,
    List<CategoryResponse> Categories,
    int ChapterCount);

public record BookDetail(
    Guid Id,
    string Title,
    string? Excerpt,
    string? Publisher,
    string? Price,
    string Language,
    string? CoverUrl,
    string? DocumentUrl,
    int Position,
    DateTime? PublishedAt,
    bool Published,
    bool IsOfflineAvailable,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<AuthorResponse> Authors,
    List<CategoryResponse> Categories,
    List<ChapterResponse> Chapters,
    SiblingRef? Previous = null,
    SiblingRef? Next = null);

public record SaveBookRequest(
    string Title,
    string? Excerpt,
    string? Publisher,
    string? Price,
    string Language,
    string? CoverUrl,
    string? DocumentUrl,
    int? Position,
    DateTime? PublishedAt,
    bool Published,
    List<Guid> AuthorIds,
    List<Guid> CategoryIds);

public record SetOfflineAvailabilityRequest(bool IsOfflineAvailable);

public record BookAuthorOption(Guid Id, string Name, int Count);

public record BookCategoryOption(Guid Id, string Title, int Count);

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
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<AuthorResponse> Authors,
    List<CategoryResponse> Categories,
    List<ChapterResponse> Chapters);

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

namespace IslamiJindegiApi.DTOs;

public record BayanListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    string? Location,
    string? AudioUrl,
    bool Published,
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
    DateTime PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories);

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

public record BayanAuthorOption(Guid Id, string Name, int Count);

public record BayanCategoryOption(Guid Id, string Title, int Count);

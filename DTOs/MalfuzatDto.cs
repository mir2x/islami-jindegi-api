namespace IslamiJindegiApi.DTOs;

public record MalfuzatListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
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
    DateTime? PublishedAt,
    int? Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse Author,
    List<CategoryResponse> Categories);

public record MalfuzatAuthorOption(Guid Id, string Name, int Count);
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

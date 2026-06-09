namespace IslamiJindegiApi.DTOs;

public record ArticleListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse? Author,
    List<CategoryResponse> Categories);

public record ArticleDetail(
    Guid Id,
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    string? DocumentUrl,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse? Author,
    List<CategoryResponse> Categories);

public record SaveArticleRequest(
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    string? DocumentUrl,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
    Guid? AuthorId,
    List<Guid> CategoryIds);

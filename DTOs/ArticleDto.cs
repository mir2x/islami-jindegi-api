namespace IslamiJindegiApi.DTOs;

public record ArticleListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    bool Published,
    bool IsOfflineAvailable,
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
    bool IsOfflineAvailable,
    DateTime? PublishedAt,
    int? Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse? Author,
    List<CategoryResponse> Categories,
    SiblingRef? Previous = null,
    SiblingRef? Next = null);

public record ArticleAuthorOption(Guid Id, string Name, int Count);
public record ArticleCategoryOption(Guid Id, string Title, int Count);

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

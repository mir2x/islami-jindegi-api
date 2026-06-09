namespace IslamiJindegiApi.DTOs;

public record MasailListItem(
    Guid Id,
    string Title,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    bool Published,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse? Author,
    List<CategoryResponse> Categories);

public record MasailDetail(
    Guid Id,
    string Title,
    string Question,
    string? Answer,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuthorResponse? Author,
    List<CategoryResponse> Categories);

public record SaveMasailRequest(
    string Title,
    string Question,
    string? Answer,
    string Language,
    bool HasAudio,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    DateTime? PublishedAt,
    int? Position,
    Guid? AuthorId,
    List<Guid> CategoryIds);

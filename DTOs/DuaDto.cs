namespace IslamiJindegiApi.DTOs;

public record DuaListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    string? AudioUrl,
    bool Published,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CategoryResponse> Categories);

public record DuaDetail(
    Guid Id,
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CategoryResponse> Categories);

public record DuaCategoryOption(Guid Id, string Title, int Count);

public record SaveDuaRequest(
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    string? AudioUrl,
    string? DocumentUrl,
    bool Published,
    int? Position,
    List<Guid> CategoryIds);

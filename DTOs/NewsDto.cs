namespace IslamiJindegiApi.DTOs;

public record NewsListItem(
    Guid Id,
    string Title,
    string? Excerpt,
    string Language,
    bool Published,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record NewsDetail(
    Guid Id,
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    bool Published,
    DateTime? PublishedAt,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SaveNewsRequest(
    string Title,
    string Body,
    string? Excerpt,
    string Language,
    bool Published,
    DateTime? PublishedAt,
    int? Position);

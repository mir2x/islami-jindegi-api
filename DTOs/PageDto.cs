namespace IslamiJindegiApi.DTOs;

public record PageListItem(
    Guid Id,
    string Title,
    string Slug,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PageDetail(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SavePageRequest(
    string Title,
    string Slug,
    string Body,
    string? ImageUrl);

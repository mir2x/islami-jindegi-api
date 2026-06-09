namespace IslamiJindegiApi.DTOs;

public record MediaResponse(
    Guid Id,
    string FileName,
    string Url,
    string Type,
    string MimeType,
    long Size,
    int? Width,
    int? Height,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

namespace IslamiJindegiApi.DTOs;

public record AuthorResponse(
    Guid Id,
    string Name,
    string? Info,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateAuthorRequest(string Name, string? Info, int? Position);

public record UpdateAuthorRequest(string Name, string? Info, int? Position);

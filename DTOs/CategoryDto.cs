namespace IslamiJindegiApi.DTOs;

public record CategoryResponse(
    Guid Id,
    string Title,
    int Position,
    Guid? ParentId,
    List<CategoryResponse> Children,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateCategoryRequest(string Title, int? Position, Guid? ParentId);

public record UpdateCategoryRequest(string Title, int? Position, Guid? ParentId);

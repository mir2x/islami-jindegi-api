namespace IslamiJindegiApi.DTOs;

public record CategoryModuleOption(string Module, int Position);

/// <summary>
/// `Modules` is only populated by the category endpoints — the admin needs it to know which
/// module lists a category appears in. Content responses that embed categories leave it null
/// rather than paying for the join.
/// </summary>
public record CategoryResponse(
    Guid Id,
    string Title,
    int Position,
    Guid? ParentId,
    List<CategoryResponse> Children,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CategoryModuleOption>? Modules = null);

public record CreateCategoryRequest(string Title, int? Position, Guid? ParentId, List<string>? Modules = null);

public record UpdateCategoryRequest(string Title, int? Position, Guid? ParentId, List<string>? Modules = null);

public record MergeCategoryRequest(Guid TargetId);

public record ReorderCategoriesRequest(string Module, List<Guid> CategoryIds);

/// <summary>How much content is attached, per module — shown before a destructive action.</summary>
public record CategoryUsage(string Module, int Items);

public enum CategoryWriteStatus { Ok, NotFound, DuplicateTitle, InvalidModule, InvalidTarget }

public record CategoryWriteResult(
    CategoryWriteStatus Status,
    CategoryResponse? Category = null,
    string? Message = null);

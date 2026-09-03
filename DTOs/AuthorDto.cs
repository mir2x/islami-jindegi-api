namespace IslamiJindegiApi.DTOs;

public record AuthorModuleOption(string Module, int Position);

/// <summary>
/// `Modules` is only populated by the author endpoints — the admin needs it to know which module
/// lists an author appears in. Content responses that embed their author leave it null rather
/// than paying for the join.
/// </summary>
public record AuthorResponse(
    Guid Id,
    string Name,
    string? Info,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<AuthorModuleOption>? Modules = null);

public record CreateAuthorRequest(string Name, string? Info, int? Position, List<string>? Modules = null);

public record UpdateAuthorRequest(string Name, string? Info, int? Position, List<string>? Modules = null);

public record MergeAuthorRequest(Guid TargetId);

public record ReorderAuthorsRequest(string Module, List<Guid> AuthorIds);

/// <summary>How much content is attributed to the author, per module — shown before a destructive action.</summary>
public record AuthorUsage(string Module, int Items);

public enum AuthorWriteStatus { Ok, NotFound, DuplicateName, InvalidModule, InvalidTarget, HasContent }

public record AuthorWriteResult(
    AuthorWriteStatus Status,
    AuthorResponse? Author = null,
    string? Message = null);

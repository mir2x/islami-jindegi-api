namespace IslamiJindegiApi.DTOs;

public record PagedResult<T>(IEnumerable<T> Data, int Total, int Page, int PageSize);

/// <summary>Minimal navigation target embedded in a content detail response.</summary>
public record SiblingRef(Guid Id, string Title, int Position);

/// <summary>Navigation target in a book's depth-first reading sequence.</summary>
public record BookNodeRef(Guid Id, string Title, int ReadingOrder, string Kind);

namespace IslamiJindegiApi.DTOs;

public record ChapterResponse(
    Guid Id,
    string Title,
    string? Body,
    int Position,
    List<SubChapterResponse> SubChapters);

// Richer response for GET /api/chapters/{id} — includes book info for edit forms
public record ChapterDetail(
    Guid Id,
    string Title,
    string? Body,
    int Position,
    Guid BookId,
    string BookTitle,
    List<SubChapterResponse> SubChapters);

public record SubChapterResponse(Guid Id, string Title, string? Body, int Position, Guid? ParentSubChapterId);

// Richer response for GET /api/subchapters/{id} — includes chapter/book info for edit forms
public record SubChapterDetail(
    Guid Id,
    string Title,
    string? Body,
    int Position,
    Guid ChapterId,
    string ChapterTitle,
    Guid BookId,
    string BookTitle,
    Guid? ParentSubChapterId);

public record ChapterListItem(Guid Id, string Title, int Position, Guid BookId, string BookTitle, int SubChapterCount);

public record SubChapterListItem(Guid Id, string Title, int Position, Guid ChapterId, string ChapterTitle, Guid BookId, string BookTitle, Guid? ParentSubChapterId);

public record SaveChapterRequest(string Title, string? Body, int? Position);

public record SaveSubChapterRequest(string Title, string? Body, int? Position, Guid? ChapterId, Guid? ParentSubChapterId);

public record CreateSubChapterRequest(string Title, string? Body, int? Position, Guid ChapterId, Guid? ParentSubChapterId);

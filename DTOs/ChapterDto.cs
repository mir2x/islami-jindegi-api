namespace IslamiJindegiApi.DTOs;

public record ChapterResponse(
    Guid Id,
    string Title,
    string? Body,
    int Position,
    List<SubChapterResponse> SubChapters);

public record SubChapterResponse(Guid Id, string Title, string Body, int Position);

public record SaveChapterRequest(string Title, string? Body, int? Position);

public record SaveSubChapterRequest(string Title, string Body, int? Position);

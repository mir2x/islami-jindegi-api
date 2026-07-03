using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IChapterService
{
    Task<PagedResult<ChapterListItem>> GetChaptersAsync(int page, int pageSize, Guid? bookId, string? search, string? sort);
    Task<PagedResult<SubChapterListItem>> GetSubChaptersAsync(int page, int pageSize, Guid? bookId, string? search, string? sort);
    Task<IEnumerable<ChapterResponse>> GetChaptersByBookAsync(Guid bookId);
    Task<ChapterDetail?> GetChapterByIdAsync(Guid id);
    Task<SubChapterDetail?> GetSubChapterByIdAsync(Guid id);
    Task<(ChapterResponse? Chapter, bool BookNotFound)> CreateChapterAsync(Guid bookId, SaveChapterRequest req);
    Task<ChapterResponse?> UpdateChapterAsync(Guid id, SaveChapterRequest req);
    Task<bool> DeleteChapterAsync(Guid id);
    Task<(SubChapterResponse? Sub, bool ChapterNotFound)> CreateSubChapterAsync(CreateSubChapterRequest req);
    Task<(SubChapterResponse? Sub, bool ChapterNotFound)> CreateSubChapterUnderChapterAsync(Guid chapterId, SaveSubChapterRequest req);
    Task<SubChapterResponse?> UpdateSubChapterAsync(Guid id, SaveSubChapterRequest req);
    Task<bool> DeleteSubChapterAsync(Guid id);
}

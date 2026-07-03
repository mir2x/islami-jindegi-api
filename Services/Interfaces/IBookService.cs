using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IBookService
{
    Task<PagedResult<BookListItem>> GetListAsync(int page, int pageSize, string? search, Guid? authorId, Guid? categoryId, bool? published, string? sort);
    Task<IEnumerable<BookAuthorOption>> GetAuthorsAsync(bool published);
    Task<IEnumerable<BookCategoryOption>> GetCategoriesAsync(bool published);
    Task<BookDetail?> GetByIdAsync(Guid id);
    Task<BookListItem> CreateAsync(SaveBookRequest req);
    Task<BookListItem?> UpdateAsync(Guid id, SaveBookRequest req);
    Task<bool> DeleteAsync(Guid id);
}

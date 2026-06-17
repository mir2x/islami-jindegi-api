using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;

namespace IslamiJindegiApi;

public static class Mappers
{
    public static AuthorResponse ToAuthorResponse(Author a) =>
        new(a.Id, a.Name, a.Info, a.Position, a.CreatedAt, a.UpdatedAt);

    public static CategoryResponse ToCategoryResponse(Category c) =>
        new(c.Id, c.Title, c.Position, c.ParentId,
            c.Children.OrderBy(ch => ch.Position).Select(ToCategoryResponse).ToList(),
            c.CreatedAt, c.UpdatedAt);

    public static ChapterResponse ToChapterResponse(Chapter c) => new(
        c.Id, c.Title, c.Body, c.Position,
        c.SubChapters.OrderBy(s => s.Position).Select(ToSubChapterResponse).ToList());

    public static SubChapterResponse ToSubChapterResponse(SubChapter s) =>
        new(s.Id, s.Title, s.Body, s.Position, s.ParentSubChapterId);

    public static MediaResponse ToMediaResponse(Media m) => new(
        m.Id, m.FileName, m.Url, m.Type, m.MimeType, m.Size,
        m.Width, m.Height, m.Description, m.CreatedAt, m.UpdatedAt);

    public static BookListItem ToBookListItem(Book b, int chapterCount = 0) => new(
        b.Id, b.Title, b.Excerpt, b.Publisher, b.Price, b.Language,
        b.CoverUrl, b.DocumentUrl, b.Position, b.PublishedAt, b.Published,
        b.CreatedAt, b.UpdatedAt,
        b.Authors.Select(ToAuthorResponse).ToList(),
        b.Categories.Select(ToCategoryResponse).ToList(),
        chapterCount);

    public static BookDetail ToBookDetail(Book b) => new(
        b.Id, b.Title, b.Excerpt, b.Publisher, b.Price, b.Language,
        b.CoverUrl, b.DocumentUrl, b.Position, b.PublishedAt, b.Published,
        b.CreatedAt, b.UpdatedAt,
        b.Authors.Select(ToAuthorResponse).ToList(),
        b.Categories.Select(ToCategoryResponse).ToList(),
        b.Chapters.OrderBy(c => c.Position).Select(ToChapterResponse).ToList());

    public static BayanListItem ToBayanListItem(Bayan b) => new(
        b.Id, b.Title, b.Excerpt, b.Language, b.Location, b.AudioUrl,
        b.Published, b.PublishedAt, b.Position, b.CreatedAt, b.UpdatedAt,
        ToAuthorResponse(b.Author),
        b.Categories.Select(ToCategoryResponse).ToList());

    public static BayanDetail ToBayanDetail(Bayan b) => new(
        b.Id, b.Title, b.Excerpt, b.Language, b.Location, b.AudioUrl,
        b.Published, b.PublishedAt, b.Position, b.CreatedAt, b.UpdatedAt,
        ToAuthorResponse(b.Author),
        b.Categories.Select(ToCategoryResponse).ToList());

    public static MalfuzatListItem ToMalfuzatListItem(Malfuzat m) => new(
        m.Id, m.Title, m.Excerpt, m.Language, m.HasAudio, m.AudioUrl,
        m.Published, m.PublishedAt, m.Position, m.CreatedAt, m.UpdatedAt,
        ToAuthorResponse(m.Author),
        m.Categories.Select(ToCategoryResponse).ToList());

    public static MalfuzatDetail ToMalfuzatDetail(Malfuzat m) => new(
        m.Id, m.Title, m.Body, m.Excerpt, m.Language, m.HasAudio, m.AudioUrl, m.DocumentUrl,
        m.Published, m.PublishedAt, m.Position, m.CreatedAt, m.UpdatedAt,
        ToAuthorResponse(m.Author),
        m.Categories.Select(ToCategoryResponse).ToList());

    public static MasailListItem ToMasailListItem(Masail m) => new(
        m.Id, m.Title, m.Language, m.HasAudio, m.AudioUrl,
        m.Published, m.PublishedAt, m.Position, m.CreatedAt, m.UpdatedAt,
        m.Author is null ? null : ToAuthorResponse(m.Author),
        m.Categories.Select(ToCategoryResponse).ToList());

    public static MasailDetail ToMasailDetail(Masail m) => new(
        m.Id, m.Title, m.Question, m.Answer, m.Language, m.HasAudio, m.AudioUrl, m.DocumentUrl,
        m.Published, m.PublishedAt, m.Position, m.CreatedAt, m.UpdatedAt,
        m.Author is null ? null : ToAuthorResponse(m.Author),
        m.Categories.Select(ToCategoryResponse).ToList());

    public static DuaListItem ToDuaListItem(Dua d) => new(
        d.Id, d.Title, d.Excerpt, d.Language, d.AudioUrl,
        d.Published, d.Position, d.CreatedAt, d.UpdatedAt,
        d.Categories.Select(ToCategoryResponse).ToList());

    public static DuaDetail ToDuaDetail(Dua d) => new(
        d.Id, d.Title, d.Body, d.Excerpt, d.Language, d.AudioUrl, d.DocumentUrl,
        d.Published, d.Position, d.CreatedAt, d.UpdatedAt,
        d.Categories.Select(ToCategoryResponse).ToList());

    public static ArticleListItem ToArticleListItem(Article a) => new(
        a.Id, a.Title, a.Excerpt, a.Language,
        a.Published, a.PublishedAt, a.Position, a.CreatedAt, a.UpdatedAt,
        a.Author is null ? null : ToAuthorResponse(a.Author),
        a.Categories.Select(ToCategoryResponse).ToList());

    public static ArticleDetail ToArticleDetail(Article a) => new(
        a.Id, a.Title, a.Body, a.Excerpt, a.Language, a.DocumentUrl,
        a.Published, a.PublishedAt, a.Position, a.CreatedAt, a.UpdatedAt,
        a.Author is null ? null : ToAuthorResponse(a.Author),
        a.Categories.Select(ToCategoryResponse).ToList());

    public static NewsDetail ToNewsDetail(News n) => new(
        n.Id, n.Title, n.Body, n.Excerpt, n.Language,
        n.Published, n.PublishedAt, n.Position, n.CreatedAt, n.UpdatedAt);

    public static MadrasahDetail ToMadrasahDetail(Madrasah m) => new(
        m.Id, m.Title, m.Excerpt, m.Introduction, m.Position,
        m.Infos.OrderBy(i => i.Position).Select(i => new MadrasahInfoItem(i.Id, i.Label, i.Info, i.Position)).ToList(),
        m.Photos.OrderBy(p => p.Position).Select(p => new MadrasahPhotoItem(p.Id, p.Title, p.ImageUrl, p.Position)).ToList(),
        m.CreatedAt, m.UpdatedAt);

    public static NamazTimeDetail ToNamazTimeDetail(NamazTime n) => new(
        n.Id, n.Title, n.TitleBn, n.Masail, n.Fazail, n.Position, n.CreatedAt, n.UpdatedAt);
}

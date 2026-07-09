using IslamiJindegiApi.DTOs;

namespace IslamiJindegiApi.Services;

public interface IQuranService
{
    IEnumerable<SurahInfo> GetSurahs();
    Task<object?> GetSurahAyahsAsync(int surahNumber, string? translations, bool words, string? tafsirs);
    Task<object?> GetAyahAsync(int surahNumber, int ayahNumber, string? translations, bool words, string? tafsirs);
    Task<PagedResult<object>> SearchAsync(string query, int page, int pageSize);
    IEnumerable<MushafEditionConfig> GetMushafs();
    MushafEditionConfig? GetMushaf(string editionId);
    (string Url, Task<long> SizeTask) GetMushafUrl(string mushafId);
    (string Url, Task<long> SizeTask) GetTafsirUrl(string tafsirId);
    (string Url, Task<long> SizeTask) GetDbUrl(string dbName);
    Task<object> GetSuraAudioUrlsAsync(string reciterId, int sura);
    IEnumerable<object> GetReciters();
    IEnumerable<object> GetTafsirs();
    Task<IEnumerable<string>> GetTranslatorsAsync();
    bool IsValidMushaf(string id);
    bool IsValidTafsir(string id);
    bool IsValidDb(string id);
    bool IsValidReciter(string id);
}

namespace IslamiJindegiApi.Services;

public interface IQuranService
{
    IEnumerable<SurahInfo> GetSurahs();
    Task<object?> GetSurahAyahsAsync(int surahNumber, string? translator, string? tafsir);
    IEnumerable<MushafEditionConfig> GetMushafs();
    MushafEditionConfig? GetMushaf(string editionId);
    (string Url, Task<long> SizeTask) GetMushafUrl(string mushafId);
    (string Url, Task<long> SizeTask) GetTafsirUrl(string tafsirId);
    (string Url, Task<long> SizeTask) GetDbUrl(string dbName);
    Task<object> GetSuraAudioUrlsAsync(string reciterId, int sura);
    bool IsValidMushaf(string id);
    bool IsValidTafsir(string id);
    bool IsValidDb(string id);
}

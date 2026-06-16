using System.ComponentModel.DataAnnotations.Schema;

namespace IslamiJindegiApi.Models;

[Table("quran_ayahs")]
public class QuranAyah
{
    public int Id { get; set; }
    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }
    public string ArabicText { get; set; } = "";

    public ICollection<QuranTranslation> Translations { get; set; } = [];
    public ICollection<QuranWord> Words { get; set; } = [];
}

[Table("quran_translations")]
public class QuranTranslation
{
    public int Id { get; set; }
    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }
    public string TranslatorName { get; set; } = "";
    public string TranslationText { get; set; } = "";
}

[Table("quran_words")]
public class QuranWord
{
    public int Id { get; set; }
    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }
    public int WordId { get; set; }
    public string ArabicWord { get; set; } = "";
    public string BengaliWord { get; set; } = "";
}

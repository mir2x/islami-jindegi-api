using System.Text.RegularExpressions;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public record SurahInfo(
    int Number, string NameArabic, string NameBengali,
    string NameEnglish, string Transliteration,
    int TotalAyahs, string RevelationType, int ParaNumber);

public record MushafEditionConfig(
    string Id, string Title, int Width, int Height,
    string Ext, int TotalPages, string PagesBaseUrl, string AyahBoxesUrl);

public class QuranService(AppDbContext db, StorageService storage) : IQuranService
{
    const string CdnBase = "https://static.islamijindegi.com";

    static readonly HashSet<string> ValidMushafs =
    [
        "imdadia_hafezi", "hafezi", "colorful_tajweed",
        "madani", "nurani", "colorful_hafezi",
    ];

    static readonly HashSet<string> ValidTafsirs =
    [
        "tafsir_taqi_usmani_bn", "tafsir_ibn_kathir_bn",
        "tafsir_ibn_kathir_en", "tafsir_maariful_quran_bn",
        "tafsir_maariful_quran_en",
    ];

    public static readonly Dictionary<string, string> TafsirTitles = new()
    {
        ["tafsir_taqi_usmani_bn"]     = "তাকী উসমানী",
        ["tafsir_ibn_kathir_bn"]      = "ইবনে কাছীর (বাংলা)",
        ["tafsir_ibn_kathir_en"]      = "Ibn Kathir (English)",
        ["tafsir_maariful_quran_bn"]  = "মা'আরিফুল কুরআন (বাংলা)",
        ["tafsir_maariful_quran_en"]  = "Maariful Quran (English)",
    };

    static readonly HashSet<string> ValidDbs =
    [
        "articles", "bayans", "books", "duas",
        "madrasahs", "malfuzats", "masails", "misc",
    ];

    public static readonly Dictionary<string, string> ReciterTitles = new()
    {
        ["abdullah-al-joohani"]                = "আব্দুল্লাহ আল জুহানী",
        ["abdur-rahman-al-sudais"]              = "আব্দুর রহমান আল সুদাইস",
        ["farees-abbad"]                        = "ফারিস আব্বাদ",
        ["mishary-bin-rashid-alafasy"]          = "মিশারি রাশিদ আলাফাসি",
        ["qari-abdul-basit"]                    = "আব্দুল বাসিত আব্দুস সামাদ",
        ["qari-maher-al-muaiqly"]               = "মাহের আল মুয়াইক্বিলি",
        ["qari-saud-bin-ibrahim-ash-shuraim"]   = "সৌদ আল-শুরাইম",
    };

    static readonly int[] AyahCounts =
    [
          7, 286, 200, 176, 120, 165, 206,  75, 129, 109,
        123, 111,  43,  52,  99, 128, 111, 110,  98, 135,
        112,  78, 118,  64,  77, 227,  93,  88,  69,  60,
         34,  30,  73,  54,  45,  83, 182,  88,  75,  85,
         54,  53,  89,  59,  37,  35,  38,  29,  18,  45,
         60,  49,  62,  55,  78,  96,  29,  22,  24,  13,
         14,  11,  11,  18,  12,  12,  30,  52,  52,  44,
         28,  28,  20,  56,  40,  31,  50,  40,  46,  42,
         29,  19,  36,  25,  22,  17,  19,  26,  30,  20,
         15,  21,  11,   8,   8,  19,   5,   8,   8,  11,
         11,   8,   3,   9,   5,   4,   7,   3,   6,   3,
          5,   4,   5,   6,
    ];

    static MushafEditionConfig BuildConfig(string id, string title, int w, int h, string ext, int totalPages) => new(
        Id: id, Title: title, Width: w, Height: h, Ext: ext, TotalPages: totalPages,
        PagesBaseUrl: $"{CdnBase}/mushaf/{id}/pages",
        AyahBoxesUrl: $"{CdnBase}/mushaf/{id}/ayah_boxes.json");

    static readonly MushafEditionConfig[] Editions =
    [
        BuildConfig("imdadia_hafezi",   "হাফিজি কুরআন (এমদাদিয়া লাইব্রেরী)", 1152, 2048, "jpg", 612),
        BuildConfig("hafezi",           "হাফিজি কুরআন",                          1152, 2048, "png", 612),
        BuildConfig("colorful_tajweed", "রঙিন তাজবীদ কুরআন",                     720,  1057, "png", 850),
        BuildConfig("madani",           "মাদানী কুরআন (উসমানী প্রিন্ট)",          1352, 2170, "png", 606),
        BuildConfig("nurani",           "নূরানী কুরআন (এমদাদিয়া লাইব্রেরী)",    670,  996,  "png", 730),
        BuildConfig("colorful_hafezi",  "রঙিন হাফিজি",                            560,  829,  "jpg", 612),
    ];

    public static readonly SurahInfo[] SurahList =
    [
        new(1,  "الفاتحة",  "আল-ফাতিহা",    "The Opening",         "Al-Fatihah",    7,   "Meccan",   1),
        new(2,  "البقرة",   "আল-বাকারা",     "The Cow",             "Al-Baqarah",    286, "Medinan",  1),
        new(3,  "آل عمران", "আলে-ইমরান",     "Family of Imran",     "Ali 'Imran",    200, "Medinan",  3),
        new(4,  "النساء",   "আন-নিসা",       "The Women",           "An-Nisa",       176, "Medinan",  4),
        new(5,  "المائدة",  "আল-মায়িদা",    "The Table Spread",    "Al-Ma'idah",    120, "Medinan",  6),
        new(6,  "الأنعام",  "আল-আনআম",       "The Cattle",          "Al-An'am",      165, "Meccan",   7),
        new(7,  "الأعراف",  "আল-আরাফ",       "The Heights",         "Al-A'raf",      206, "Meccan",   8),
        new(8,  "الأنفال",  "আল-আনফাল",      "The Spoils of War",   "Al-Anfal",      75,  "Medinan",  9),
        new(9,  "التوبة",   "আত-তাওবা",      "The Repentance",      "At-Tawbah",     129, "Medinan",  10),
        new(10, "يونس",     "ইউনুস",          "Jonah",               "Yunus",         109, "Meccan",   11),
        new(11, "هود",      "হুদ",            "Hud",                 "Hud",           123, "Meccan",   11),
        new(12, "يوسف",     "ইউসুফ",          "Joseph",              "Yusuf",         111, "Meccan",   12),
        new(13, "الرعد",    "আর-রাদ",         "The Thunder",         "Ar-Ra'd",       43,  "Medinan",  13),
        new(14, "إبراهيم",  "ইবরাহীম",        "Abraham",             "Ibrahim",       52,  "Meccan",   13),
        new(15, "الحجر",    "আল-হিজর",        "The Rocky Tract",     "Al-Hijr",       99,  "Meccan",   14),
        new(16, "النحل",    "আন-নাহল",        "The Bee",             "An-Nahl",       128, "Meccan",   14),
        new(17, "الإسراء",  "আল-ইসরা",        "The Night Journey",   "Al-Isra",       111, "Meccan",   15),
        new(18, "الكهف",    "আল-কাহফ",        "The Cave",            "Al-Kahf",       110, "Meccan",   15),
        new(19, "مريم",     "মারইয়াম",        "Mary",                "Maryam",        98,  "Meccan",   16),
        new(20, "طه",       "তা-হা",           "Ta-Ha",               "Ta-Ha",         135, "Meccan",   16),
        new(21, "الأنبياء", "আল-আম্বিয়া",    "The Prophets",        "Al-Anbiya",     112, "Meccan",   17),
        new(22, "الحج",     "আল-হজ্জ",        "The Pilgrimage",      "Al-Hajj",       78,  "Medinan",  17),
        new(23, "المؤمنون", "আল-মুমিনুন",     "The Believers",       "Al-Mu'minun",   118, "Meccan",   18),
        new(24, "النور",    "আন-নূর",          "The Light",           "An-Nur",        64,  "Medinan",  18),
        new(25, "الفرقان",  "আল-ফুরকান",      "The Criterion",       "Al-Furqan",     77,  "Meccan",   18),
        new(26, "الشعراء",  "আশ-শুআরা",       "The Poets",           "Ash-Shu'ara",   227, "Meccan",   19),
        new(27, "النمل",    "আন-নামল",         "The Ant",             "An-Naml",       93,  "Meccan",   19),
        new(28, "القصص",    "আল-কাসাস",        "The Stories",         "Al-Qasas",      88,  "Meccan",   20),
        new(29, "العنكبوت", "আল-আনকাবুত",     "The Spider",          "Al-'Ankabut",   69,  "Meccan",   20),
        new(30, "الروم",    "আর-রুম",          "The Romans",          "Ar-Rum",        60,  "Meccan",   21),
        new(31, "لقمان",    "লুকমান",          "Luqman",              "Luqman",        34,  "Meccan",   21),
        new(32, "السجدة",   "আস-সাজদা",        "The Prostration",     "As-Sajdah",     30,  "Meccan",   21),
        new(33, "الأحزاب",  "আল-আহযাব",        "The Combined Forces", "Al-Ahzab",      73,  "Medinan",  21),
        new(34, "سبأ",      "সাবা",            "Sheba",               "Saba",          54,  "Meccan",   22),
        new(35, "فاطر",     "ফাতির",           "Originator",          "Fatir",         45,  "Meccan",   22),
        new(36, "يس",       "ইয়া-সীন",         "Ya-Sin",              "Ya-Sin",        83,  "Meccan",   22),
        new(37, "الصافات",  "আস-সাফফাত",       "Those Who Set The Ranks", "As-Saffat", 182, "Meccan",   23),
        new(38, "ص",        "সাদ",             "The Letter Sad",      "Sad",           88,  "Meccan",   23),
        new(39, "الزمر",    "আয-যুমার",        "The Troops",          "Az-Zumar",      75,  "Meccan",   23),
        new(40, "غافر",     "গাফির",           "The Forgiver",        "Ghafir",        85,  "Meccan",   24),
        new(41, "فصلت",     "ফুসসিলাত",        "Explained in Detail", "Fussilat",      54,  "Meccan",   24),
        new(42, "الشورى",   "আশ-শূরা",         "The Consultation",    "Ash-Shura",     53,  "Meccan",   25),
        new(43, "الزخرف",   "আয-যুখরুফ",       "The Ornaments of Gold","Az-Zukhruf",   89,  "Meccan",   25),
        new(44, "الدخان",   "আদ-দুখান",        "The Smoke",           "Ad-Dukhan",     59,  "Meccan",   25),
        new(45, "الجاثية",  "আল-জাসিয়া",      "The Crouching",       "Al-Jathiyah",   37,  "Meccan",   25),
        new(46, "الأحقاف",  "আল-আহকাফ",        "The Wind-Curved Sandhills","Al-Ahqaf", 35,  "Meccan",   26),
        new(47, "محمد",     "মুহাম্মদ",         "Muhammad",            "Muhammad",      38,  "Medinan",  26),
        new(48, "الفتح",    "আল-ফাতহ",         "The Victory",         "Al-Fath",       29,  "Medinan",  26),
        new(49, "الحجرات",  "আল-হুজুরাত",      "The Rooms",           "Al-Hujurat",    18,  "Medinan",  26),
        new(50, "ق",        "কাফ",             "The Letter Qaf",      "Qaf",           45,  "Meccan",   26),
        new(51, "الذاريات", "আয-যারিয়াত",     "The Winnowing Winds", "Adh-Dhariyat",  60,  "Meccan",   26),
        new(52, "الطور",    "আত-তূর",          "The Mount",           "At-Tur",        49,  "Meccan",   27),
        new(53, "النجم",    "আন-নাজম",          "The Star",            "An-Najm",       62,  "Meccan",   27),
        new(54, "القمر",    "আল-কামার",         "The Moon",            "Al-Qamar",      55,  "Meccan",   27),
        new(55, "الرحمن",   "আর-রাহমান",        "The Beneficent",      "Ar-Rahman",     78,  "Medinan",  27),
        new(56, "الواقعة",  "আল-ওয়াকিআ",       "The Inevitable",      "Al-Waqi'ah",    96,  "Meccan",   27),
        new(57, "الحديد",   "আল-হাদীদ",         "The Iron",            "Al-Hadid",      29,  "Medinan",  27),
        new(58, "المجادلة", "আল-মুজাদালা",      "The Pleading Woman",  "Al-Mujadila",   22,  "Medinan",  28),
        new(59, "الحشر",    "আল-হাশর",          "The Exile",           "Al-Hashr",      24,  "Medinan",  28),
        new(60, "الممتحنة", "আল-মুমতাহিনা",     "She that is to be Examined","Al-Mumtahanah",13,"Medinan",28),
        new(61, "الصف",     "আস-সাফ",           "The Ranks",           "As-Saf",        14,  "Medinan",  28),
        new(62, "الجمعة",   "আল-জুমআ",          "The Congregation",    "Al-Jumu'ah",    11,  "Medinan",  28),
        new(63, "المنافقون","আল-মুনাফিকুন",     "The Hypocrites",      "Al-Munafiqun",  11,  "Medinan",  28),
        new(64, "التغابن",  "আত-তাগাবুন",       "The Mutual Disillusion","At-Taghabun",  18,  "Medinan",  28),
        new(65, "الطلاق",   "আত-তালাক",         "The Divorce",         "At-Talaq",      12,  "Medinan",  28),
        new(66, "التحريم",  "আত-তাহরীম",        "The Prohibition",     "At-Tahrim",     12,  "Medinan",  28),
        new(67, "الملك",    "আল-মুলক",           "The Sovereignty",     "Al-Mulk",       30,  "Meccan",   29),
        new(68, "القلم",    "আল-কালাম",          "The Pen",             "Al-Qalam",      52,  "Meccan",   29),
        new(69, "الحاقة",   "আল-হাক্কা",         "The Reality",         "Al-Haqqah",     52,  "Meccan",   29),
        new(70, "المعارج",  "আল-মাআরিজ",         "The Ascending Stairways","Al-Ma'arij", 44,  "Meccan",   29),
        new(71, "نوح",      "নূহ",               "Noah",                "Nuh",           28,  "Meccan",   29),
        new(72, "الجن",     "আল-জিন",            "The Jinn",            "Al-Jinn",       28,  "Meccan",   29),
        new(73, "المزمل",   "আল-মুযযাম্মিল",     "The Enshrouded One",  "Al-Muzzammil",  20,  "Meccan",   29),
        new(74, "المدثر",   "আল-মুদ্দাস্সির",    "The Cloaked One",     "Al-Muddaththir",56,  "Meccan",   29),
        new(75, "القيامة",  "আল-কিয়ামা",         "The Resurrection",    "Al-Qiyamah",    40,  "Meccan",   29),
        new(76, "الإنسان",  "আল-ইনসান",           "The Man",             "Al-Insan",      31,  "Medinan",  29),
        new(77, "المرسلات", "আল-মুরসালাত",        "The Emissaries",      "Al-Mursalat",   50,  "Meccan",   29),
        new(78, "النبأ",    "আন-নাবা",            "The Tidings",         "An-Naba",       40,  "Meccan",   30),
        new(79, "النازعات", "আন-নাযিআত",           "Those Who Drag Forth","An-Nazi'at",   46,  "Meccan",   30),
        new(80, "عبس",      "আবাসা",              "He Frowned",          "'Abasa",        42,  "Meccan",   30),
        new(81, "التكوير",  "আত-তাকওয়ীর",         "The Overthrowing",    "At-Takwir",     29,  "Meccan",   30),
        new(82, "الانفطار", "আল-ইনফিতার",          "The Cleaving",        "Al-Infitar",    19,  "Meccan",   30),
        new(83, "المطففين", "আল-মুতাফফিফীন",       "The Defrauding",      "Al-Mutaffifin", 36,  "Meccan",   30),
        new(84, "الانشقاق", "আল-ইনশিকাক",          "The Sundering",       "Al-Inshiqaq",   25,  "Meccan",   30),
        new(85, "البروج",   "আল-বুরুজ",            "The Mansions of the Stars","Al-Buruj", 22,  "Meccan",   30),
        new(86, "الطارق",   "আত-তারিক",            "The Morning Star",    "At-Tariq",      17,  "Meccan",   30),
        new(87, "الأعلى",   "আল-আলা",              "The Most High",       "Al-A'la",       19,  "Meccan",   30),
        new(88, "الغاشية",  "আল-গাশিয়া",           "The Overwhelming",    "Al-Ghashiyah",  26,  "Meccan",   30),
        new(89, "الفجر",    "আল-ফাজর",             "The Dawn",            "Al-Fajr",       30,  "Meccan",   30),
        new(90, "البلد",    "আল-বালাদ",             "The City",            "Al-Balad",      20,  "Meccan",   30),
        new(91, "الشمس",    "আশ-শামস",              "The Sun",             "Ash-Shams",     15,  "Meccan",   30),
        new(92, "الليل",    "আল-লাইল",              "The Night",           "Al-Layl",       21,  "Meccan",   30),
        new(93, "الضحى",    "আদ-দুহা",              "The Morning Hours",   "Ad-Duha",       11,  "Meccan",   30),
        new(94, "الشرح",    "আশ-শারহ",              "The Relief",          "Ash-Sharh",     8,   "Meccan",   30),
        new(95, "التين",    "আত-তীন",               "The Fig",             "At-Tin",        8,   "Meccan",   30),
        new(96, "العلق",    "আল-আলাক",              "The Clot",            "Al-'Alaq",      19,  "Meccan",   30),
        new(97, "القدر",    "আল-কাদর",              "The Power",           "Al-Qadr",       5,   "Meccan",   30),
        new(98, "البينة",   "আল-বাইয়িনা",           "The Clear Proof",     "Al-Bayyinah",   8,   "Medinan",  30),
        new(99, "الزلزلة",  "আয-যিলযাল",            "The Earthquake",      "Az-Zalzalah",   8,   "Medinan",  30),
        new(100,"العاديات", "আল-আদিয়াত",            "The Courser",         "Al-'Adiyat",    11,  "Meccan",   30),
        new(101,"القارعة",  "আল-কারিআ",              "The Calamity",        "Al-Qari'ah",    11,  "Meccan",   30),
        new(102,"التكاثر",  "আত-তাকাসুর",            "The Rivalry in World Increase","At-Takathur",8,"Meccan",30),
        new(103,"العصر",    "আল-আসর",                "The Declining Day",   "Al-'Asr",       3,   "Meccan",   30),
        new(104,"الهمزة",   "আল-হুমাযা",             "The Traducer",        "Al-Humazah",    9,   "Meccan",   30),
        new(105,"الفيل",    "আল-ফীল",                "The Elephant",        "Al-Fil",        5,   "Meccan",   30),
        new(106,"قريش",     "কুরাইশ",                "Quraysh",             "Quraysh",       4,   "Meccan",   30),
        new(107,"الماعون",  "আল-মাউন",               "The Small Kindnesses","Al-Ma'un",      7,   "Meccan",   30),
        new(108,"الكوثر",   "আল-কাওসার",             "The Abundance",       "Al-Kawthar",    3,   "Meccan",   30),
        new(109,"الكافرون", "আল-কাফিরুন",            "The Disbelievers",    "Al-Kafirun",    6,   "Meccan",   30),
        new(110,"النصر",    "আন-নাসর",               "The Divine Support",  "An-Nasr",       3,   "Medinan",  30),
        new(111,"المسد",    "আল-মাসাদ",              "The Palm Fiber",      "Al-Masad",      5,   "Meccan",   30),
        new(112,"الإخلاص",  "আল-ইখলাস",              "The Sincerity",       "Al-Ikhlas",     4,   "Meccan",   30),
        new(113,"الفلق",    "আল-ফালাক",              "The Daybreak",        "Al-Falaq",      5,   "Meccan",   30),
        new(114,"الناس",    "আন-নাস",                "Mankind",             "An-Nas",        6,   "Meccan",   30),
    ];

    public IEnumerable<SurahInfo> GetSurahs() => SurahList;

    public MushafEditionConfig? GetMushaf(string editionId) =>
        Editions.FirstOrDefault(e => e.Id == editionId);

    public IEnumerable<MushafEditionConfig> GetMushafs() => Editions;

    public bool IsValidMushaf(string id) => ValidMushafs.Contains(id);
    public bool IsValidTafsir(string id) => ValidTafsirs.Contains(id);
    public bool IsValidDb(string id) => ValidDbs.Contains(id);
    public bool IsValidReciter(string id) => ReciterTitles.ContainsKey(id);

    public IEnumerable<object> GetReciters() =>
        ReciterTitles.Select(kv => new { id = kv.Key, name = kv.Value });

    public IEnumerable<object> GetTafsirs() =>
        TafsirTitles.Select(kv => new { id = kv.Key, name = kv.Value });

    public async Task<IEnumerable<string>> GetTranslatorsAsync() =>
        await db.QuranTranslations.Select(t => t.TranslatorName).Distinct().OrderBy(n => n).ToListAsync();

    public (string Url, Task<long> SizeTask) GetMushafUrl(string mushafId)
    {
        var key = $"assets/al-quran/mushafs/{mushafId}.zip";
        return (storage.GetPresignedUrl(key, 3600), storage.GetFileSizeAsync(key));
    }

    public (string Url, Task<long> SizeTask) GetTafsirUrl(string tafsirId)
    {
        var key = $"assets/al-quran/tafsirs/{tafsirId}.json";
        return (storage.GetPresignedUrl(key, 3600), storage.GetFileSizeAsync(key));
    }

    public (string Url, Task<long> SizeTask) GetDbUrl(string dbName)
    {
        var key = $"assets/db/{dbName}.sqlite3";
        return (storage.GetPresignedUrl(key, 3600), storage.GetFileSizeAsync(key));
    }

    public async Task<object> GetSuraAudioUrlsAsync(string reciterId, int sura)
    {
        var totalAyahs = AyahCounts[sura - 1];
        var keys = Enumerable.Range(1, totalAyahs)
            .Select(a => $"assets/al-quran/qirats/{reciterId}/{sura}/{a}.mp3")
            .ToList();

        var urls = keys.Select(k => storage.GetPresignedUrl(k, 300)).ToList();
        var sizes = await Task.WhenAll(keys.Select(k => storage.GetFileSizeAsync(k)));

        var totalBytes = sizes.Sum();
        return new
        {
            urls,
            totalAyahs,
            totalDownloadSizeBytes = totalBytes,
            totalDownloadSizeMB = Math.Round(totalBytes / (1024.0 * 1024.0), 2),
        };
    }

    // "none" -> exclude, "all" or null/empty -> everything, otherwise a comma-separated allow-list.
    static IQueryable<QuranTranslation> FilterTranslations(IQueryable<QuranTranslation> q, string? filter)
    {
        if (filter == "none") return q.Where(_ => false);
        if (string.IsNullOrEmpty(filter) || filter == "all") return q;
        var names = filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return q.Where(t => names.Contains(t.TranslatorName));
    }

    // Same semantics as FilterTranslations, but tafsirs default to excluded (payloads are large).
    static IQueryable<QuranTafsir> FilterTafsirs(IQueryable<QuranTafsir> q, string? filter)
    {
        if (string.IsNullOrEmpty(filter) || filter == "none") return q.Where(_ => false);
        if (filter == "all") return q;
        var ids = filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return q.Where(t => ids.Contains(t.TafsirId));
    }

    static object ToTranslationDto(QuranTranslation t) => new { translator = t.TranslatorName, text = t.TranslationText };
    static object ToWordDto(QuranWord w) => new { id = w.WordId, arabic = w.ArabicWord, bengali = w.BengaliWord };
    static object ToTafsirDto(QuranTafsir t) => new { id = t.TafsirId, name = TafsirTitles.GetValueOrDefault(t.TafsirId, t.TafsirId), text = t.TafsirText };

    /// <summary>
    /// translations/tafsirs: null/"all" = everything, "none" = excluded, or a comma-separated allow-list
    /// (translator names / tafsir ids respectively). words: whether to include the word-by-word breakdown.
    /// </summary>
    public async Task<object?> GetSurahAyahsAsync(int surahNumber, string? translations, bool words, string? tafsirs)
    {
        if (surahNumber < 1 || surahNumber > 114) return null;
        var surah = SurahList.FirstOrDefault(s => s.Number == surahNumber);
        if (surah is null) return null;

        var ayahs = await db.QuranAyahs
            .Where(a => a.SurahNumber == surahNumber)
            .OrderBy(a => a.AyahNumber)
            .ToListAsync();

        var translationRows = await FilterTranslations(db.QuranTranslations.Where(t => t.SurahNumber == surahNumber), translations)
            .ToListAsync();

        var wordRows = words
            ? await db.QuranWords.Where(w => w.SurahNumber == surahNumber).OrderBy(w => w.AyahNumber).ThenBy(w => w.WordId).ToListAsync()
            : [];

        var tafsirRows = await FilterTafsirs(db.QuranTafsirs.Where(t => t.SurahNumber == surahNumber), tafsirs)
            .ToListAsync();

        var translationsByAyah = translationRows.GroupBy(t => t.AyahNumber).ToDictionary(g => g.Key, g => g.ToList());
        var wordsByAyah = wordRows.GroupBy(w => w.AyahNumber).ToDictionary(g => g.Key, g => g.ToList());
        var tafsirsByAyah = tafsirRows.GroupBy(t => t.AyahNumber).ToDictionary(g => g.Key, g => g.ToList());

        var result = ayahs.Select(a => new
        {
            number = a.AyahNumber,
            arabic = a.ArabicText,
            translations = translationsByAyah.TryGetValue(a.AyahNumber, out var ts) ? ts.Select(ToTranslationDto).ToList() : [],
            words = wordsByAyah.TryGetValue(a.AyahNumber, out var ws) ? ws.Select(ToWordDto).ToList() : [],
            tafsirs = tafsirsByAyah.TryGetValue(a.AyahNumber, out var tfs) ? tfs.Select(ToTafsirDto).ToList() : [],
        });

        return new
        {
            surahNumber = surah.Number,
            nameBengali = surah.NameBengali,
            nameArabic = surah.NameArabic,
            nameEnglish = surah.NameEnglish,
            transliteration = surah.Transliteration,
            totalAyahs = surah.TotalAyahs,
            revelationType = surah.RevelationType,
            paraNumber = surah.ParaNumber,
            ayahs = result,
        };
    }

    /// <summary>Same filter semantics as GetSurahAyahsAsync, scoped to a single ayah.</summary>
    public async Task<object?> GetAyahAsync(int surahNumber, int ayahNumber, string? translations, bool words, string? tafsirs)
    {
        if (surahNumber < 1 || surahNumber > 114) return null;
        var surah = SurahList.FirstOrDefault(s => s.Number == surahNumber);
        if (surah is null || ayahNumber < 1 || ayahNumber > surah.TotalAyahs) return null;

        var ayah = await db.QuranAyahs.FirstOrDefaultAsync(a => a.SurahNumber == surahNumber && a.AyahNumber == ayahNumber);
        if (ayah is null) return null;

        var translationRows = await FilterTranslations(
            db.QuranTranslations.Where(t => t.SurahNumber == surahNumber && t.AyahNumber == ayahNumber), translations).ToListAsync();

        var wordRows = words
            ? await db.QuranWords.Where(w => w.SurahNumber == surahNumber && w.AyahNumber == ayahNumber).OrderBy(w => w.WordId).ToListAsync()
            : [];

        var tafsirRows = await FilterTafsirs(
            db.QuranTafsirs.Where(t => t.SurahNumber == surahNumber && t.AyahNumber == ayahNumber), tafsirs).ToListAsync();

        return new
        {
            surahNumber = surah.Number,
            ayahNumber = ayah.AyahNumber,
            arabic = ayah.ArabicText,
            translations = translationRows.Select(ToTranslationDto).ToList(),
            words = wordRows.Select(ToWordDto).ToList(),
            tafsirs = tafsirRows.Select(ToTafsirDto).ToList(),
        };
    }

    // Arabic combining diacritics (harakat, tanwin, shadda, sukun, quranic annotation marks) + tatweel.
    static readonly Regex ArabicDiacritics = new(
        "[\u064B-\u065F\u0670\u06D6-\u06DC\u06DF-\u06E4\u06E7-\u06E8\u06EA-\u06ED\u0640]",
        RegexOptions.Compiled);
    static string NormalizeArabic(string s) => ArabicDiacritics.Replace(s, "");

    public async Task<PagedResult<object>> SearchAsync(string query, int page, int pageSize)
    {
        var arabicQuery = NormalizeArabic(query);

        var arabicMatches = db.QuranAyahs
            .Where(a => a.ArabicTextPlain != null && EF.Functions.ILike(a.ArabicTextPlain, $"%{arabicQuery}%"))
            .Select(a => new { a.SurahNumber, a.AyahNumber });

        var translationMatches = db.QuranTranslations
            .Where(t => EF.Functions.ILike(t.TranslationText, $"%{query}%"))
            .Select(t => new { t.SurahNumber, t.AyahNumber });

        var matchedKeys = await arabicMatches.Union(translationMatches)
            .OrderBy(k => k.SurahNumber).ThenBy(k => k.AyahNumber)
            .ToListAsync();

        var total = matchedKeys.Count;
        var pageKeyList = matchedKeys.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(k => (k.SurahNumber, k.AyahNumber))
            .ToList();
        var pageKeySet = pageKeyList.ToHashSet();
        var surahNumbersOnPage = pageKeyList.Select(k => k.SurahNumber).ToHashSet();

        // Fetch the superset (all ayahs/translations for surahs represented on this page), then
        // narrow to the exact matched (surah, ayah) pairs in memory — composite-key IN-filtering
        // doesn't translate reliably to SQL, and the dataset is small enough (6.2k ayahs total)
        // for this to be cheap regardless.
        var ayahsByKey = (await db.QuranAyahs.Where(a => surahNumbersOnPage.Contains(a.SurahNumber)).ToListAsync())
            .Where(a => pageKeySet.Contains((a.SurahNumber, a.AyahNumber)))
            .ToDictionary(a => (a.SurahNumber, a.AyahNumber));

        var translationsByKey = (await db.QuranTranslations.Where(t => surahNumbersOnPage.Contains(t.SurahNumber)).ToListAsync())
            .Where(t => pageKeySet.Contains((t.SurahNumber, t.AyahNumber)))
            .GroupBy(t => (t.SurahNumber, t.AyahNumber))
            .ToDictionary(g => g.Key, g => g.ToList());

        var hits = pageKeyList.Select(k => (object)new
        {
            surahNumber = k.SurahNumber,
            surahName = SurahList.First(s => s.Number == k.SurahNumber).NameBengali,
            ayahNumber = k.AyahNumber,
            arabic = ayahsByKey.TryGetValue(k, out var ayah) ? ayah.ArabicText : "",
            translations = translationsByKey.TryGetValue(k, out var ts) ? ts.Select(ToTranslationDto).ToList() : [],
        });

        return new PagedResult<object>(hits, total, page, pageSize);
    }
}

using IslamiJindegiApi.Data;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class QuranTextEndpoints
{
    public static void MapQuranTextEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/quran");

        g.MapGet("/surahs", () => Results.Ok(Surahs));

        g.MapGet("/surahs/{number:int}/ayahs", async (int number, string? translator, AppDbContext db) =>
        {
            if (number < 1 || number > 114)
                return Results.NotFound();

            var surah = Surahs.FirstOrDefault(s => s.Number == number);
            if (surah is null) return Results.NotFound();

            var ayahs = await db.QuranAyahs
                .Where(a => a.SurahNumber == number)
                .OrderBy(a => a.AyahNumber)
                .ToListAsync();

            var translations = await db.QuranTranslations
                .Where(t => t.SurahNumber == number)
                .ToListAsync();

            var words = await db.QuranWords
                .Where(w => w.SurahNumber == number)
                .OrderBy(w => w.AyahNumber).ThenBy(w => w.WordId)
                .ToListAsync();

            var translationsByAyah = translations
                .GroupBy(t => t.AyahNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            var wordsByAyah = words
                .GroupBy(w => w.AyahNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = ayahs.Select(a => new
            {
                number = a.AyahNumber,
                arabic = a.ArabicText,
                translations = translationsByAyah.TryGetValue(a.AyahNumber, out var ts)
                    ? ts
                        .Where(t => translator == null || t.TranslatorName == translator)
                        .Select(t => new { translator = t.TranslatorName, text = t.TranslationText })
                        .ToList()
                    : [],
                words = wordsByAyah.TryGetValue(a.AyahNumber, out var ws)
                    ? ws.Select(w => new { id = w.WordId, arabic = w.ArabicWord, bengali = w.BengaliWord }).ToList()
                    : [],
            });

            return Results.Ok(new
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
            });
        });
    }

    // ── 114 Surahs ────────────────────────────────────────────────────────────

    public record SurahInfo(
        int Number, string NameArabic, string NameBengali,
        string NameEnglish, string Transliteration,
        int TotalAyahs, string RevelationType, int ParaNumber);

    public static readonly SurahInfo[] Surahs =
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
}

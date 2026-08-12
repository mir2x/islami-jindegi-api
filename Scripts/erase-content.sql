-- Erase all migrated content in the NEW system before re-running --migrate-data.
-- Deliberately NOT touched: "Admins", "HijriMonthSightings",
-- quran_ayahs, quran_translations, quran_words, quran_tafsirs.
--
-- Run with: psql "$NEW_URL" -v ON_ERROR_STOP=1 -f Scripts/erase-content.sql

BEGIN;

TRUNCATE TABLE
  book_authors,
  book_categories,
  article_categories,
  bayan_categories,
  dua_categories,
  malfuzat_categories,
  masail_categories,
  "SubChapters",
  "Chapters",
  "Books",
  "Bayans",
  "Malfuzats",
  "Masails",
  "Duas",
  "Articles",
  "News",
  "MadrasahPhotos",
  "MadrasahInfos",
  "Madrasahs",
  "NamazTimes",
  "Pages",
  "Medias",
  "Authors",
  "Categories"
CASCADE;

COMMIT;

-- Sanity check: content tables should all be 0, preserved tables unchanged.
SELECT 'Authors' t, COUNT(*) FROM "Authors" UNION ALL
SELECT 'Categories', COUNT(*) FROM "Categories" UNION ALL
SELECT 'Books', COUNT(*) FROM "Books" UNION ALL
SELECT 'Bayans', COUNT(*) FROM "Bayans" UNION ALL
SELECT 'Medias', COUNT(*) FROM "Medias" UNION ALL
SELECT 'Pages', COUNT(*) FROM "Pages" UNION ALL
SELECT 'Admins (kept)', COUNT(*) FROM "Admins" UNION ALL
SELECT 'HijriSightings (kept)', COUNT(*) FROM "HijriMonthSightings" UNION ALL
SELECT 'quran_ayahs (kept)', COUNT(*) FROM quran_ayahs UNION ALL
SELECT 'quran_tafsirs (kept)', COUNT(*) FROM quran_tafsirs;

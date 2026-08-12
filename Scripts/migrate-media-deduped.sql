-- Rebuild the media library from content URLs, ONE row per unique file.
-- A file shared by multiple content items (e.g. same audio in a Bayan and a
-- Malfuzat) produces a single Media row; the description comes from its
-- earliest usage (by content CreatedAt).
--
-- Compared to the old migrate-media.sql this also includes MadrasahPhotos
-- and Pages images, and dedupes by StorageKey.
--
-- Run AFTER --migrate-data and --migrate-pages, with "Medias" empty.
-- Run with: psql "$NEW_URL" -v ON_ERROR_STOP=1 -f Scripts/migrate-media-deduped.sql

BEGIN;

WITH sources AS (
  SELECT "CoverUrl" AS url, 'image' AS type,
         'Image used in Book ' || "Title" AS context, "CreatedAt" AS created_at
  FROM "Books" WHERE COALESCE("CoverUrl", '') <> ''

  UNION ALL
  SELECT "DocumentUrl", 'document',
         'Document used in Book ' || "Title", "CreatedAt"
  FROM "Books" WHERE COALESCE("DocumentUrl", '') <> ''

  UNION ALL
  SELECT b."AudioUrl", 'audio',
         'Audio used in Bayan by ' || COALESCE(a."Name", 'Unknown'), b."CreatedAt"
  FROM "Bayans" b
  LEFT JOIN "Authors" a ON a."Id" = b."AuthorId"
  WHERE COALESCE(b."AudioUrl", '') <> ''

  UNION ALL
  SELECT "AudioUrl", 'audio',
         'Audio used in Malfuzat ' || "Title", "CreatedAt"
  FROM "Malfuzats" WHERE COALESCE("AudioUrl", '') <> '' AND "HasAudio" = true

  UNION ALL
  SELECT "DocumentUrl", 'document',
         'Document used in Malfuzat ' || "Title", "CreatedAt"
  FROM "Malfuzats" WHERE COALESCE("DocumentUrl", '') <> ''

  UNION ALL
  SELECT "AudioUrl", 'audio',
         'Audio used in Masail ' || "Title", "CreatedAt"
  FROM "Masails" WHERE COALESCE("AudioUrl", '') <> '' AND "HasAudio" = true

  UNION ALL
  SELECT "DocumentUrl", 'document',
         'Document used in Masail ' || "Title", "CreatedAt"
  FROM "Masails" WHERE COALESCE("DocumentUrl", '') <> ''

  UNION ALL
  SELECT "AudioUrl", 'audio',
         'Audio used in Dua ' || "Title", "CreatedAt"
  FROM "Duas" WHERE COALESCE("AudioUrl", '') <> ''

  UNION ALL
  SELECT "DocumentUrl", 'document',
         'Document used in Dua ' || "Title", "CreatedAt"
  FROM "Duas" WHERE COALESCE("DocumentUrl", '') <> ''

  UNION ALL
  SELECT "DocumentUrl", 'document',
         'Document used in Article ' || "Title", "CreatedAt"
  FROM "Articles" WHERE COALESCE("DocumentUrl", '') <> ''

  UNION ALL
  SELECT "ImageUrl", 'image',
         'Photo used in Madrasah ' || "Title", "CreatedAt"
  FROM "MadrasahPhotos" WHERE COALESCE("ImageUrl", '') <> ''

  UNION ALL
  SELECT "ImageUrl", 'image',
         'Image used in Page ' || "Title", "CreatedAt"
  FROM "Pages" WHERE COALESCE("ImageUrl", '') <> ''
),
keyed AS (
  SELECT replace(url, 'https://static.islamijindegi.com/uploads/store/', '') AS storage_key,
         url, type, context, created_at
  FROM sources
),
dedup AS (
  SELECT DISTINCT ON (storage_key) storage_key, url, type, context, created_at
  FROM keyed
  ORDER BY storage_key, created_at ASC
)
INSERT INTO "Medias"
  ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  CASE type WHEN 'image' THEN 'img-' WHEN 'document' THEN 'doc-' ELSE 'audio-' END
    || to_char(created_at AT TIME ZONE 'UTC', 'DD-MM-YY'),
  storage_key,
  url,
  type,
  CASE lower(reverse(split_part(reverse(url), '.', 1)))
    WHEN 'jpg'  THEN 'image/jpeg'
    WHEN 'jpeg' THEN 'image/jpeg'
    WHEN 'png'  THEN 'image/png'
    WHEN 'webp' THEN 'image/webp'
    WHEN 'gif'  THEN 'image/gif'
    WHEN 'pdf'  THEN 'application/pdf'
    WHEN 'mp3'  THEN 'audio/mpeg'
    WHEN 'mp4'  THEN 'audio/mp4'
    WHEN 'm4a'  THEN 'audio/x-m4a'
    WHEN 'ogg'  THEN 'audio/ogg'
    WHEN 'wav'  THEN 'audio/wav'
    WHEN 'webm' THEN 'audio/webm'
    ELSE CASE type
      WHEN 'image'    THEN 'image/jpeg'
      WHEN 'document' THEN 'application/pdf'
      ELSE 'audio/mpeg'
    END
  END,
  0,
  context || ' (' || to_char(created_at AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  created_at,
  NOW()
FROM dedup;

COMMIT;

-- Verify: total rows, per-type counts, and confirm no duplicate storage keys.
SELECT "Type", COUNT(*) FROM "Medias" GROUP BY "Type" ORDER BY "Type";
SELECT COUNT(*) AS total_media FROM "Medias";
SELECT COUNT(*) AS duplicate_keys
FROM (SELECT "StorageKey" FROM "Medias" GROUP BY "StorageKey" HAVING COUNT(*) > 1) d;

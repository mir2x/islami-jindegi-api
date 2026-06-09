-- Migrate existing file URLs from all content modules into the Medias table.
-- StorageKey = URL path after 'https://static.islamijindegi.com/uploads/store/'
-- Size set to 0 (unknown without downloading); Width/Height NULL.
-- Run AFTER applying the AddMediaDescription migration.

-- ============================================================
-- BOOKS — Cover images
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'img-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("CoverUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "CoverUrl",
  'image',
  CASE lower(reverse(split_part(reverse("CoverUrl"), '.', 1)))
    WHEN 'jpg'  THEN 'image/jpeg'
    WHEN 'jpeg' THEN 'image/jpeg'
    WHEN 'png'  THEN 'image/png'
    WHEN 'webp' THEN 'image/webp'
    WHEN 'gif'  THEN 'image/gif'
    ELSE 'image/jpeg'
  END,
  0,
  'Image used in Book ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Books"
WHERE "CoverUrl" IS NOT NULL AND "CoverUrl" != '';

-- ============================================================
-- BOOKS — Documents (PDF)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'doc-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("DocumentUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "DocumentUrl",
  'document',
  'application/pdf',
  0,
  'Document used in Book ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Books"
WHERE "DocumentUrl" IS NOT NULL AND "DocumentUrl" != '';

-- ============================================================
-- BAYANS — Audio (join Authors for speaker name)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'audio-' || to_char(b."CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace(b."AudioUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  b."AudioUrl",
  'audio',
  CASE lower(reverse(split_part(reverse(b."AudioUrl"), '.', 1)))
    WHEN 'mp3'  THEN 'audio/mpeg'
    WHEN 'mp4'  THEN 'audio/mp4'
    WHEN 'm4a'  THEN 'audio/x-m4a'
    WHEN 'ogg'  THEN 'audio/ogg'
    WHEN 'wav'  THEN 'audio/wav'
    WHEN 'webm' THEN 'audio/webm'
    ELSE 'audio/mpeg'
  END,
  0,
  'Audio used in Bayan by ' || COALESCE(a."Name", 'Unknown') || ' (' || to_char(b."CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  b."CreatedAt",
  NOW()
FROM "Bayans" b
LEFT JOIN "Authors" a ON a."Id" = b."AuthorId"
WHERE b."AudioUrl" IS NOT NULL AND b."AudioUrl" != '';

-- ============================================================
-- MALFUZATS — Audio
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'audio-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("AudioUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "AudioUrl",
  'audio',
  CASE lower(reverse(split_part(reverse("AudioUrl"), '.', 1)))
    WHEN 'mp3'  THEN 'audio/mpeg'
    WHEN 'mp4'  THEN 'audio/mp4'
    WHEN 'm4a'  THEN 'audio/x-m4a'
    WHEN 'ogg'  THEN 'audio/ogg'
    WHEN 'wav'  THEN 'audio/wav'
    WHEN 'webm' THEN 'audio/webm'
    ELSE 'audio/mpeg'
  END,
  0,
  'Audio used in Malfuzat ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Malfuzats"
WHERE "AudioUrl" IS NOT NULL AND "AudioUrl" != '' AND "HasAudio" = true;

-- ============================================================
-- MALFUZATS — Documents (PDF)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'doc-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("DocumentUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "DocumentUrl",
  'document',
  'application/pdf',
  0,
  'Document used in Malfuzat ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Malfuzats"
WHERE "DocumentUrl" IS NOT NULL AND "DocumentUrl" != '';

-- ============================================================
-- MASAILS — Audio
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'audio-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("AudioUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "AudioUrl",
  'audio',
  CASE lower(reverse(split_part(reverse("AudioUrl"), '.', 1)))
    WHEN 'mp3'  THEN 'audio/mpeg'
    WHEN 'mp4'  THEN 'audio/mp4'
    WHEN 'm4a'  THEN 'audio/x-m4a'
    WHEN 'ogg'  THEN 'audio/ogg'
    WHEN 'wav'  THEN 'audio/wav'
    WHEN 'webm' THEN 'audio/webm'
    ELSE 'audio/mpeg'
  END,
  0,
  'Audio used in Masail ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Masails"
WHERE "AudioUrl" IS NOT NULL AND "AudioUrl" != '' AND "HasAudio" = true;

-- ============================================================
-- MASAILS — Documents (PDF)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'doc-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("DocumentUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "DocumentUrl",
  'document',
  'application/pdf',
  0,
  'Document used in Masail ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Masails"
WHERE "DocumentUrl" IS NOT NULL AND "DocumentUrl" != '';

-- ============================================================
-- DUAS — Audio
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'audio-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("AudioUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "AudioUrl",
  'audio',
  CASE lower(reverse(split_part(reverse("AudioUrl"), '.', 1)))
    WHEN 'mp3'  THEN 'audio/mpeg'
    WHEN 'mp4'  THEN 'audio/mp4'
    WHEN 'm4a'  THEN 'audio/x-m4a'
    WHEN 'ogg'  THEN 'audio/ogg'
    WHEN 'wav'  THEN 'audio/wav'
    WHEN 'webm' THEN 'audio/webm'
    ELSE 'audio/mpeg'
  END,
  0,
  'Audio used in Dua ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Duas"
WHERE "AudioUrl" IS NOT NULL AND "AudioUrl" != '';

-- ============================================================
-- DUAS — Documents (PDF)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'doc-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("DocumentUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "DocumentUrl",
  'document',
  'application/pdf',
  0,
  'Document used in Dua ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Duas"
WHERE "DocumentUrl" IS NOT NULL AND "DocumentUrl" != '';

-- ============================================================
-- ARTICLES — Documents (PDF)
-- ============================================================
INSERT INTO "Medias" ("Id", "FileName", "StorageKey", "Url", "Type", "MimeType", "Size", "Description", "CreatedAt", "UpdatedAt")
SELECT
  gen_random_uuid(),
  'doc-' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY'),
  replace("DocumentUrl", 'https://static.islamijindegi.com/uploads/store/', ''),
  "DocumentUrl",
  'document',
  'application/pdf',
  0,
  'Document used in Article ' || "Title" || ' (' || to_char("CreatedAt" AT TIME ZONE 'UTC', 'DD-MM-YY') || ')',
  "CreatedAt",
  NOW()
FROM "Articles"
WHERE "DocumentUrl" IS NOT NULL AND "DocumentUrl" != '';

-- ============================================================
-- Verify counts
-- ============================================================
SELECT type, COUNT(*) FROM "Medias" GROUP BY type ORDER BY type;
SELECT COUNT(*) AS total FROM "Medias";

-- 06-merge-duplicate-authors.sql
-- Consolidates the duplicate author rows so `ix_authors_name_normalized` can be created.
--
-- Authors were being consolidated by RENAMING one onto another in the admin, which moves no
-- content. The result is the same signature the categories had: two rows with one name, the
-- content split between them. "মুফতী মনসূরুল হক সাহেব" is the worst case -- two byte-identical
-- rows, one holding the bayan and article, the other the book, malfuzat and masail.
--
-- Nothing is deleted here except the duplicate AUTHOR rows, and only after every piece of their
-- content has been repointed. Verified against the 2026-09-04 production snapshot:
--   * none of the six rows carries an `Info` value, so no attribute has to be reconciled
--   * no book carries both members of any pair, so the join-table collapse drops zero links
--
-- `UpdatedAt` is bumped on every moved row deliberately: the offline sync endpoints are delta
-- queries on `UpdatedAt` and they embed the whole author object, so without the bump every phone
-- would keep serving the deleted author's id and name for content it already holds. Only 223 of
-- the moved rows are offline-available, so the re-sync is small.
--
-- Run BEFORE the AddAuthorModules migration (the unique index it creates fails while duplicates
-- remain, deliberately) and before Scripts/07.
\set ON_ERROR_STOP on
BEGIN;

CREATE TEMP TABLE author_merges(src uuid PRIMARY KEY, tgt uuid NOT NULL, tier text, note text) ON COMMIT DROP;

-- Tier A -- the same name twice. These MUST be merged: the unique index cannot be created
-- while they exist.
INSERT INTO author_merges VALUES
  ('cd8e5363-0276-4822-b495-bda3f076791d','2ce83449-8dab-4215-8aaa-9e97a6a7ddf1','A',
   'মুফতী মনসূরুল হক সাহেব -- byte-identical; source created 2021 from speakers, renamed in the admin 2026-08-25'),
  ('83035113-af6c-4239-b994-685f3832a6d3','1fefbba9-d6ac-4215-9f39-167d913ce87d','A',
   'মুফতী মীযানুর রহমান কাসেমী সাহেব (রাহমানিয়া) -- differs only by an invisible ZWJ (U+200D)'),
  ('2acf2218-0e09-40bb-947a-0487646bd968','1add4742-51e1-43d9-82db-c4ed9c9b788c','A',
   'মাওলানা সৈয়দ আবুল হাসান আলী নদভী রহ. -- differs only by Bengali composition (U+09DF vs U+09AF U+09BC)');

-- Tier B -- merges MigrateDataCommand's own name maps declare, but which never executed. Both
-- sides are still separate rows; one shaykh is currently three rows with 313 items between them.
INSERT INTO author_merges VALUES
  ('277bcf30-cb63-42b9-ac7d-cf0dce031d7f','90ddb1a6-b89e-474c-a472-3ffb65f85c9c','B',
   'MalfuzatAuthorMap: হযরত সাইয়্যিদ মাওলানা আবরারুল হক সাহেব রহ. -> শাহ সাইয়্যিদ আবরারুল হক সাহেব'),
  ('dae2aa71-f1a7-4cdd-9959-804a4140f5e7','90ddb1a6-b89e-474c-a472-3ffb65f85c9c','B',
   'BayanAuthorMap: হযরত সাইয়্যিদ মাওলানা শাহ আবরারুল হক সাহেব রহ. -> শাহ সাইয়্যিদ আবরারুল হক সাহেব'),
  ('e1354973-e92a-4cbe-b653-f8396a74ef99','3bfb5f0c-8476-40fa-92d5-c724670e9058','B',
   'BayanAuthorMap: মাওলানা ইউসুফ কান্ধলভী রহ. (২য় হযরতজী) -> হযরতজী মাওলানা ইউসুফ কান্ধলভী রহ.');

-- Deliberately NOT merged: 'সাইয়্যিদ আবরারুল হক রহ.-এর কিতাবের তালীম' (bfa0292a, 42 bayans) is a
-- programme name, not the shaykh, and sits right beside the three আবরারুল হক rows above. Six
-- further near-duplicate pairs found by trigram matching are open questions for the owner --
-- see docs/author-analysis.md.

DO $$
DECLARE n int;
BEGIN
  SELECT count(*) INTO n FROM author_merges a JOIN author_merges b ON a.tgt = b.src;
  IF n <> 0 THEN RAISE EXCEPTION 'chained merge: a target is also a source'; END IF;

  SELECT count(*) INTO n FROM author_merges m
  WHERE NOT EXISTS (SELECT 1 FROM "Authors" a WHERE a."Id" = m.src)
     OR NOT EXISTS (SELECT 1 FROM "Authors" a WHERE a."Id" = m.tgt);
  IF n <> 0 THEN RAISE EXCEPTION '% merge pair(s) reference an author that does not exist', n; END IF;
END $$;

-- Books are many-to-many. ON CONFLICT DO NOTHING is required, not defensive: a book could carry
-- both authors, and the join table has a composite primary key.
INSERT INTO book_authors ("BooksId", "AuthorsId")
SELECT ba."BooksId", m.tgt FROM book_authors ba JOIN author_merges m ON m.src = ba."AuthorsId"
ON CONFLICT DO NOTHING;
DELETE FROM book_authors ba USING author_merges m WHERE ba."AuthorsId" = m.src;

-- The other four modules hold a plain foreign key on the content row.
UPDATE "Bayans"    c SET "AuthorId" = m.tgt, "UpdatedAt" = now() FROM author_merges m WHERE c."AuthorId" = m.src;
UPDATE "Malfuzats" c SET "AuthorId" = m.tgt, "UpdatedAt" = now() FROM author_merges m WHERE c."AuthorId" = m.src;
UPDATE "Masails"   c SET "AuthorId" = m.tgt, "UpdatedAt" = now() FROM author_merges m WHERE c."AuthorId" = m.src;
UPDATE "Articles"  c SET "AuthorId" = m.tgt, "UpdatedAt" = now() FROM author_merges m WHERE c."AuthorId" = m.src;

-- Nothing may still point at a source row. Bayans and Malfuzats still cascade at this point
-- (AddAuthorModules has not run yet), so this check is what stands between the DELETE below and
-- thousands of deleted items.
DO $$
DECLARE n int;
BEGIN
  SELECT (SELECT count(*) FROM book_authors ba JOIN author_merges m ON m.src = ba."AuthorsId")
       + (SELECT count(*) FROM "Bayans"    c JOIN author_merges m ON m.src = c."AuthorId")
       + (SELECT count(*) FROM "Malfuzats" c JOIN author_merges m ON m.src = c."AuthorId")
       + (SELECT count(*) FROM "Masails"   c JOIN author_merges m ON m.src = c."AuthorId")
       + (SELECT count(*) FROM "Articles"  c JOIN author_merges m ON m.src = c."AuthorId")
  INTO n;
  IF n <> 0 THEN RAISE EXCEPTION '% row(s) still reference a merge source -- refusing to delete', n; END IF;
END $$;

DELETE FROM "Authors" a USING author_merges m WHERE a."Id" = m.src;

-- Strip the invisible zero-width characters from the two names that carry them. They are
-- unfixable in the admin (nobody can see them) and they are how the মীযানুর duplicate was born.
UPDATE "Authors"
SET "Name" = translate("Name", U&'\200B\200C\200D\FEFF', ''), "UpdatedAt" = now()
WHERE "Name" ~ U&'[\200B\200C\200D\FEFF]';

-- The surviving আবরারুল হক row never carried the honorific, though both rows merged into it did.
-- He is deceased, so রহ. belongs on it -- and after the merge this is the name 314 items are
-- credited to, rather than the single book it held before.
UPDATE "Authors"
SET "Name" = 'শাহ সাইয়্যিদ আবরারুল হক সাহেব রহ.', "UpdatedAt" = now()
WHERE "Id" = '90ddb1a6-b89e-474c-a472-3ffb65f85c9c';

-- Restore the spelling the old system used. MigrateDataCommand's BayanAuthorMap mapped the
-- correctly spelled name onto a typo ('মাওলাা', missing ন), and the typo is what got stored.
UPDATE "Authors"
SET "Name" = 'মাওলানা কালিম সিদ্দিকী সাহেব', "UpdatedAt" = now()
WHERE "Name" = 'মাওলাা কালীম সিদ্দীকি সাহেব';

DO $$
DECLARE n int; a int;
BEGIN
  SELECT count(*) INTO a FROM "Authors";
  IF a <> 394 THEN RAISE EXCEPTION 'expected 394 authors after merge, found %', a; END IF;

  -- content totals must be unchanged
  SELECT count(*) INTO n FROM book_authors;
  IF n <> 1070 THEN RAISE EXCEPTION 'book_authors changed: expected 1070, found %', n; END IF;
  SELECT count(*) INTO n FROM "Bayans";
  IF n <> 6124 THEN RAISE EXCEPTION 'Bayans changed: expected 6124, found %', n; END IF;
  SELECT count(*) INTO n FROM "Malfuzats";
  IF n <> 1870 THEN RAISE EXCEPTION 'Malfuzats changed: expected 1870, found %', n; END IF;
  SELECT count(*) INTO n FROM "Masails";
  IF n <> 2782 THEN RAISE EXCEPTION 'Masails changed: expected 2782, found %', n; END IF;
  SELECT count(*) INTO n FROM "Articles";
  IF n <> 184 THEN RAISE EXCEPTION 'Articles changed: expected 184, found %', n; END IF;

  -- and the unique index must now be creatable
  SELECT count(*) INTO n FROM (
    SELECT normalize(translate(btrim("Name"), U&'\2018\2019\200B\200C\200D\FEFF', ''''''), NFC) k
    FROM "Authors" GROUP BY 1 HAVING count(*) > 1) z;
  IF n <> 0 THEN RAISE EXCEPTION '% duplicate normalised name(s) remain', n; END IF;

  RAISE NOTICE 'authors merged: 6 removed, % remain', a;
END $$;

COMMIT;

-- 01-restore-ijtima.sql
-- Decision #3 (PM): ইজতিমা should not stay merged into দাওয়াত ও তাবলীগ.
--
-- The duplicate `দাওয়াত ও তাবলীগ` row holds exactly 25 bayans, and those 25 are precisely
-- the old system's ইজতিমা bayans (verified 25/25/25 exact match). So restoring the category
-- is a rename of this row -- no content moves -- and it resolves one duplicate group at the
-- same time.
\set ON_ERROR_STOP on
BEGIN;

DO $$
DECLARE n int; t text;
BEGIN
  SELECT count(*) INTO n FROM bayan_categories WHERE "CategoriesId" = 'd17a4a2c-e64c-47a5-a3b0-668b234b5494';
  SELECT "Title" INTO t FROM "Categories" WHERE "Id" = 'd17a4a2c-e64c-47a5-a3b0-668b234b5494';
  IF t IS NULL THEN RAISE EXCEPTION 'category d17a4a2c-e64c-47a5-a3b0-668b234b5494 not found'; END IF;
  IF n <> 25 THEN RAISE EXCEPTION 'expected 25 bayans, found %', n; END IF;
  RAISE NOTICE 'renaming "%" (% bayans) -> ইজতিমা', t, n;
END $$;

UPDATE "Categories" SET "Title" = 'ইজতিমা', "UpdatedAt" = now() AT TIME ZONE 'utc'
WHERE "Id" = 'd17a4a2c-e64c-47a5-a3b0-668b234b5494';

COMMIT;

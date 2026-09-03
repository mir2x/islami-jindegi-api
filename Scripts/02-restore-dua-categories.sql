-- 02-restore-dua-categories.sql
-- Decision #8 (Owner): Dua gets its original 17 categories back; দু'আ দুরূদ leaves the module.
--
-- Seven Dua categories lost their identity in the migration. Their content is intact and was
-- traced back through the old dua_categorizations rows, so each one is restored exactly.
-- Restored categories reuse their ORIGINAL old-system UUIDs (verified free of collisions).
--
-- Dua is strictly one-category-per-item (211 duas, 211 links), so this partitions cleanly.
\set ON_ERROR_STOP on
BEGIN;

-- ভূমিকা — 1 duas (old Dua position 1)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('9106ef07-f6e9-4e62-aaab-952878235240', 'ভূমিকা', 88, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, '9106ef07-f6e9-4e62-aaab-952878235240' FROM unnest(ARRAY[
    'ee483561-b6e9-4569-94cc-3beb61850cb9'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- মুনাজাত — 7 duas (old Dua position 2)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('4cc9eb94-ef78-4e4e-ba66-b20cb3808c94', 'মুনাজাত', 89, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, '4cc9eb94-ef78-4e4e-ba66-b20cb3808c94' FROM unnest(ARRAY[
    '238921d8-bdf7-464c-8aff-d7027e0fa919',
    '62eaa6c9-8822-4895-9e21-5ae34fd5628b',
    '7de3927e-b93c-4d86-9f44-f41a41ef9af3',
    'b93c3428-911e-4270-9628-781e405430f2',
    'c149964c-180c-44e5-a013-5fb3d01ad7ca',
    'cd79d333-127a-4aa9-b546-65ea104b9aaa',
    'd2d3b0e2-08fb-483a-855e-b5524b52e5ba'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- ইস্তিগফার প্রসঙ্গ — 3 duas (old Dua position 8)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('f7d46fbe-e7a7-4a26-a94d-d5dc709070d5', 'ইস্তিগফার প্রসঙ্গ', 90, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, 'f7d46fbe-e7a7-4a26-a94d-d5dc709070d5' FROM unnest(ARRAY[
    '087448b7-409b-4ff8-be9c-a68df02a866c',
    '3fb85e79-860c-4e8b-baf8-18e47f2ff7de',
    'e444ce1b-383d-42d1-9540-10f41e8c55c6'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- দুরূদ শরীফ প্রসঙ্গ — 2 duas (old Dua position 9)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('309c71ab-edce-40de-9982-0ba80a2fba5a', 'দুরূদ শরীফ প্রসঙ্গ', 91, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, '309c71ab-edce-40de-9982-0ba80a2fba5a' FROM unnest(ARRAY[
    '5fa0a4e6-1608-4669-a83c-f231868ffcf8',
    'e814c519-8ffe-4018-8d24-56fd2da25e31'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- কিছু নফল নামাযের প্রসঙ্গ — 12 duas (old Dua position 11)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('80cd7aa1-997f-4f65-994b-eab4041d303d', 'কিছু নফল নামাযের প্রসঙ্গ', 92, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, '80cd7aa1-997f-4f65-994b-eab4041d303d' FROM unnest(ARRAY[
    '03b61bbf-9bb5-420c-b4da-9ff0deaf7da0',
    '25c725f9-6d9a-4f39-8b26-b18b37101c09',
    '413d8434-cce5-4977-b443-e6f59f260d90',
    '4636153a-39f5-4757-b394-1aa30a3b0fd2',
    '7b79aeb2-b567-43bd-a5b7-c897834caa7a',
    '856ec50f-2008-494b-b6cf-f4db6e44d6f2',
    '8cfe49af-c47c-4957-bf9d-a0010b91b3c6',
    'a3881f5b-8745-434e-a524-2123626e499f',
    'b7b145e9-862a-436a-b33f-774e3ca77741',
    'bc19a368-f519-4d7a-8cc4-d5c3e7e0eb58',
    'c94a6004-c16c-4676-8392-848e06568a1c',
    'deb86c74-c788-4912-b3ab-c51c9103d250'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- অত্যন্ত ফযীলতপূর্ণ বিশেষ কিছু আমল — 7 duas (old Dua position 12)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('cdf3315f-59ec-4411-aae8-7cba6bc75542', 'অত্যন্ত ফযীলতপূর্ণ বিশেষ কিছু আমল', 93, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, 'cdf3315f-59ec-4411-aae8-7cba6bc75542' FROM unnest(ARRAY[
    '030adfa5-224c-4d8f-a265-3e5388ef486d',
    '0e25c4d3-ad28-4c38-933f-83d680938f01',
    '2d5f0287-b74f-4c1f-bd4f-c9354e5faaca',
    '455f4c92-0e4b-4ba7-a097-3f1cf281b533',
    'a5fc70af-cdc1-4975-adc6-97f193a902b6',
    'bbe91643-5483-4384-a691-f83e12706350',
    'fc5389f5-6daa-4a44-b3c1-1ca9f9a0ab39'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;
-- মুনাজাতে মাকবূল — 8 duas (old Dua position 15)
INSERT INTO "Categories" ("Id","Title","Position","ParentId","CreatedAt","UpdatedAt")
VALUES ('290a76d8-ac43-4de3-afd1-d6441a678e0c', 'মুনাজাতে মাকবূল', 94, NULL, now() AT TIME ZONE 'utc', now() AT TIME ZONE 'utc');
INSERT INTO dua_categories ("DuasId","CategoriesId")
SELECT x, '290a76d8-ac43-4de3-afd1-d6441a678e0c' FROM unnest(ARRAY[
    '28427bff-da02-4e12-88ac-f9845e1f8fc2',
    '2eadd147-f035-429c-9407-4f654f4d13ff',
    '3abcc219-3fc4-4eaf-9dfa-1bb7aeeb59ff',
    '5f4c2420-ed39-412f-89d6-04ef52eafb2b',
    '746fb50c-1890-4df6-8c43-167a493d014b',
    '89cda44c-b192-433a-98f1-514353f06787',
    'ba96de41-ca44-4cea-b7dd-cae7ace7a0a7',
    'f88a99de-9a76-455b-902f-870732723058'
  ]::uuid[]) x
ON CONFLICT DO NOTHING;

-- The four categories that absorbed this content hold no other dua content --
-- verified: their dua counts (1 + 12 + 7 + 20 = 40) are exactly the restored items.
-- Removing their dua links takes them out of the Dua module entirely.
-- (বিবিধ, নামায, বারো মাসের আমল, দু'আ দুরূদ)
DELETE FROM dua_categories WHERE "CategoriesId" IN ('d2477f7c-ee5f-4e05-9891-8318695b049c', '99e51a9a-5736-471a-b39e-e8e161a13a4f', '3207e7c6-f933-4933-9b8f-37af7e77e35f', 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591');

DO $$
DECLARE n int;
BEGIN
  SELECT count(*) INTO n FROM dua_categories;
  IF n <> 211 THEN RAISE EXCEPTION 'expected 211 dua links after restore, found %', n; END IF;
  SELECT count(DISTINCT "CategoriesId") INTO n FROM dua_categories;
  IF n <> 17 THEN RAISE EXCEPTION 'expected 17 dua categories, found %', n; END IF;
  RAISE NOTICE 'Dua restored: 17 categories, 211 links';
END $$;

COMMIT;

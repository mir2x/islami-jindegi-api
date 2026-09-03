-- 03-merge-duplicate-categories.sql
-- Decision #1 (Owner): eradicate duplicate category titles completely.
--
-- Seven titles existed more than once, five of them byte-identical. In every case the content
-- was SPLIT between the copies -- the result of staff consolidating categories by RENAMING
-- them in the admin, which changes the label but does not move content.
--
-- The lower global position is kept. ON CONFLICT DO NOTHING is required: content can already
-- carry both the source and the target category (the অপসংস্কৃতি merge has overlapping items),
-- and the join tables have a composite primary key.
--
-- Run AFTER 01 (which resolves the ইজতিমা row) and 02 (which empties the দু'আ দুরূদ duplicate).
\set ON_ERROR_STOP on
BEGIN;

-- "উলামা তোলাবা" (position 35) -> keep eb0869cf-67c6-46fb-862a-d70547d17c3a
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM book_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM bayan_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM dua_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM malfuzat_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM masail_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", 'eb0869cf-67c6-46fb-862a-d70547d17c3a' FROM article_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';
DELETE FROM "Categories" WHERE "Id" = 'a5bc12c3-202d-48c3-abab-2c1c700c8c82';

-- "মেয়েদের বিষয়" (position 50) -> keep 70bc592e-e893-4f02-a8b0-1b730c31f4b2
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM book_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM bayan_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM dua_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM malfuzat_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM masail_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", '70bc592e-e893-4f02-a8b0-1b730c31f4b2' FROM article_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';
DELETE FROM "Categories" WHERE "Id" = 'd2cdbe3b-b3ed-4f56-9cc0-17af7aeebbf7';

-- "দু'আ দুরূদ" (position 60) -> keep cf280620-90fe-4854-b4dd-1a320a4370ea
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM book_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM bayan_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM dua_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM malfuzat_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM masail_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", 'cf280620-90fe-4854-b4dd-1a320a4370ea' FROM article_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';
DELETE FROM "Categories" WHERE "Id" = 'b9b340ec-dc4e-4e0a-a9d7-8c38189d7591';

-- "দাওয়াত ও তাবলীগ" (position 75) -> keep 1dd3bbb3-7528-4fd3-be91-65f9999da2a5
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM book_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM bayan_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM dua_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM malfuzat_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM masail_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", '1dd3bbb3-7528-4fd3-be91-65f9999da2a5' FROM article_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';
DELETE FROM "Categories" WHERE "Id" = 'b4cc7d7e-d839-438b-98f8-a6f14b0984cf';

-- "আত্মশুদ্ধি" (position 77) -> keep 415f3a00-b916-4b39-9ab5-606711d1dd61
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM book_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM bayan_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM dua_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM malfuzat_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM masail_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", '415f3a00-b916-4b39-9ab5-606711d1dd61' FROM article_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = '5b629636-2927-40b7-9132-cd61faf222d6';
DELETE FROM "Categories" WHERE "Id" = '5b629636-2927-40b7-9132-cd61faf222d6';

-- "মহিলাদের বিষয়" (position 81) -> keep bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM book_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM bayan_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM dua_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM malfuzat_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM masail_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", 'bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e' FROM article_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';
DELETE FROM "Categories" WHERE "Id" = '6f43dace-0f93-464b-abf4-2c6eea75de2f';

-- "অপসংস্কৃতি ও বাতিল সম্প্রদায়" (position 84) -> keep 935dd29a-e118-4365-ad79-f670d0bb0547
INSERT INTO book_categories ("BooksId","CategoriesId") SELECT "BooksId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM book_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM book_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
INSERT INTO bayan_categories ("BayansId","CategoriesId") SELECT "BayansId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM bayan_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM bayan_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
INSERT INTO dua_categories ("DuasId","CategoriesId") SELECT "DuasId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM dua_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM dua_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
INSERT INTO malfuzat_categories ("MalfuzatsId","CategoriesId") SELECT "MalfuzatsId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM malfuzat_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM malfuzat_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
INSERT INTO masail_categories ("MasailsId","CategoriesId") SELECT "MasailsId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM masail_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM masail_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
INSERT INTO article_categories ("ArticlesId","CategoriesId") SELECT "ArticlesId", '935dd29a-e118-4365-ad79-f670d0bb0547' FROM article_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7' ON CONFLICT DO NOTHING;
DELETE FROM article_categories WHERE "CategoriesId" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';
DELETE FROM "Categories" WHERE "Id" = '8f386e5a-ad98-4616-96d3-a6bb6caaf7b7';

DO $$
DECLARE n int;
BEGIN
  SELECT count(*) INTO n FROM (
    SELECT 1 FROM "Categories" GROUP BY normalize(translate(btrim("Title"), U&'\2018\2019', ''''''), NFC) HAVING count(*) > 1) d;
  IF n <> 0 THEN RAISE EXCEPTION '% duplicate title groups remain', n; END IF;
  RAISE NOTICE 'no duplicate titles remain';
END $$;

COMMIT;

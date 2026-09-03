-- 05-seed-category-modules.sql
-- Decision #2 (Owner): per-module positions restored from the old Rails system.
--
-- Each module used to have its own category table with its own `position`. The unified
-- Categories table has ONE Position column, so five of the six modules lost their ordering --
-- only Kitab's numbers survived, because MigrateCategories copied book_categories.position
-- verbatim. Everything else got `nextPos++`, an append counter. That is why the per-module
-- list endpoints sort by content count rather than by position.
--
-- Positions here were recovered by tracing each old category to its current one through the
-- CONTENT (old *_categorizations rows against the live join tables), not through titles --
-- titles have been renamed, duplicated and mangled by apostrophe/Unicode differences. All
-- 162 old categories that held content traced at >=95% confidence.
--
-- Positions are renumbered 1..N per module, preserving the old relative order. Categories
-- with no old counterpart (curated in the new admin after the migration) are appended after
-- them, marked below.
--
-- Run AFTER Scripts/01..04 and after the AddCategoryModules migration.
-- 174 rows across 6 modules.
\set ON_ERROR_STOP on
BEGIN;

DELETE FROM category_modules;

-- Kitab — 31 categories, 1 appended with no old position
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('0c520e46-2a7c-4a3f-8680-c00aa7e1fb47', 'book', 1),  -- ঈমান আক্বাইদ
  ('4b211683-b7a8-4a63-810d-4114792624e6', 'book', 2),  -- গুনাহ ও বিদ‘আত
  ('935dd29a-e118-4365-ad79-f670d0bb0547', 'book', 3),  -- অপসংস্কৃতি ও বাতিল সম্প্রদায়
  ('632c4bba-ffa9-4ef0-b5ce-cea7b8091655', 'book', 4),  -- সুন্নত
  ('99e51a9a-5736-471a-b39e-e8e161a13a4f', 'book', 5),  -- নামায
  ('a1cebb34-4f8d-4ab4-b67f-39a9ad219dc7', 'book', 6),  -- রোযা ও ইতিকাফ
  ('2bee4ace-77f9-4e9c-ad92-f123f28189ed', 'book', 7),  -- কুরবানী ও আক্বিকা
  ('9f8dc41f-b2f6-4cde-8fb7-934ea16845da', 'book', 8),  -- হাজ্জ
  ('2257cc54-7d6c-4035-845d-67a32adfb330', 'book', 9),  -- কাফন-দাফন
  ('cf280620-90fe-4854-b4dd-1a320a4370ea', 'book', 10),  -- দু‘আ দুরূদ
  ('8281e28b-5c7e-46f5-af8e-64145d5a3909', 'book', 11),  -- লেনদেন কামাই রোজগার
  ('a8b55714-78d3-403e-97ad-120794f5d360', 'book', 12),  -- বান্দার হক
  ('d8d95a49-cd5e-4ebb-8eed-b40b46dd7271', 'book', 13),  -- বিবাহ তালাক
  ('415f3a00-b916-4b39-9ab5-606711d1dd61', 'book', 14),  -- আত্মশুদ্ধি
  ('1dd3bbb3-7528-4fd3-be91-65f9999da2a5', 'book', 15),  -- দাওয়াত ও তাবলীগ
  ('2b0c0747-5c2c-443b-88b8-1e22a863a5f0', 'book', 16),  -- পরিপূর্ণ দীন
  ('ab814e54-699a-41e2-8c9c-b24d29b936e3', 'book', 17),  -- কুরআন ও তাফসীর
  ('ce1d9d4f-c7e7-455f-832e-6ecf9636be17', 'book', 18),  -- হাদীস ও শরাহ
  ('1c31d516-5393-4667-afde-f8de1617581b', 'book', 19),  -- মাদরাসার কিতাব
  ('3f05871a-e590-494b-a935-46b8a02f8a37', 'book', 20),  -- মাসআলা-মাসাইল
  ('eb0869cf-67c6-46fb-862a-d70547d17c3a', 'book', 21),  -- উলামা তোলাবা
  ('70bc592e-e893-4f02-a8b0-1b730c31f4b2', 'book', 22),  -- মেয়েদের বিষয়
  ('27540893-41d0-4140-af40-831becbcee20', 'book', 23),  -- সিরাত ও নবী আ. এর যিন্দেগী
  ('5196e34d-7b4a-4eb6-8bb2-6671100ac506', 'book', 24),  -- সাহাবা রা. এর যিন্দেগী
  ('bcc0e436-347d-4d74-871a-1f2683415e93', 'book', 25),  -- আল্লাহওয়ালাগণের যিন্দেগী
  ('d25162ff-4359-4ce3-8ba3-b645a5212485', 'book', 26),  -- অন্যান্য ঘটনা ইতিহাস
  ('2fb02bbb-cb84-4fda-a654-87f9f999011f', 'book', 27),  -- মালফুযাত ও মাওয়ায়েজ
  ('d2477f7c-ee5f-4e05-9891-8318695b049c', 'book', 28),  -- বিবিধ
  ('92a82e4a-edc1-427d-b400-a3c3b59ccfc6', 'book', 29),  -- মাসিক পত্রিকা
  ('3207e7c6-f933-4933-9b8f-37af7e77e35f', 'book', 30),  -- বারো মাসের আমল
  ('a8cb3636-b4c3-44f0-893f-764624a9717e', 'book', 31)  -- মসজিদ মাদরাসা  [no old position — appended]
ON CONFLICT DO NOTHING;

-- Bayan — 31 categories, 1 appended with no old position
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('322906ce-fb9b-4939-9a26-6b2008588c5f', 'bayan', 1),  -- জুম‘আ
  ('bd783368-f944-45aa-8b69-07543a1773e1', 'bayan', 2),  -- মাহফিল
  ('3b0005b8-70c6-40a6-b0a0-67315f0d58f5', 'bayan', 3),  -- দাওয়াতুল হক
  ('3e9ed847-8e06-411c-89af-7940b671240a', 'bayan', 4),  -- খানকায়ে আবরারিয়া
  ('3f05871a-e590-494b-a935-46b8a02f8a37', 'bayan', 5),  -- মাসআলা-মাসাইল
  ('1dd3bbb3-7528-4fd3-be91-65f9999da2a5', 'bayan', 6),  -- দাওয়াত ও তাবলীগ
  ('ffb30b1d-b602-43d0-9568-8f0199e42266', 'bayan', 7),  -- তালীম
  ('415f3a00-b916-4b39-9ab5-606711d1dd61', 'bayan', 8),  -- আত্মশুদ্ধি
  ('d17a4a2c-e64c-47a5-a3b0-668b234b5494', 'bayan', 9),  -- ইজতিমা
  ('0c520e46-2a7c-4a3f-8680-c00aa7e1fb47', 'bayan', 10),  -- ঈমান আক্বাইদ
  ('4b211683-b7a8-4a63-810d-4114792624e6', 'bayan', 11),  -- গুনাহ ও বিদ‘আত
  ('935dd29a-e118-4365-ad79-f670d0bb0547', 'bayan', 12),  -- অপসংস্কৃতি ও বাতিল সম্প্রদায়
  ('632c4bba-ffa9-4ef0-b5ce-cea7b8091655', 'bayan', 13),  -- সুন্নত
  ('99e51a9a-5736-471a-b39e-e8e161a13a4f', 'bayan', 14),  -- নামায
  ('a1cebb34-4f8d-4ab4-b67f-39a9ad219dc7', 'bayan', 15),  -- রোযা ও ইতিকাফ
  ('2bee4ace-77f9-4e9c-ad92-f123f28189ed', 'bayan', 16),  -- কুরবানী ও আক্বিকা
  ('9f8dc41f-b2f6-4cde-8fb7-934ea16845da', 'bayan', 17),  -- হাজ্জ
  ('cf280620-90fe-4854-b4dd-1a320a4370ea', 'bayan', 18),  -- দু‘আ দুরূদ
  ('3207e7c6-f933-4933-9b8f-37af7e77e35f', 'bayan', 19),  -- বারো মাসের আমল
  ('8281e28b-5c7e-46f5-af8e-64145d5a3909', 'bayan', 20),  -- লেনদেন কামাই রোজগার
  ('a8b55714-78d3-403e-97ad-120794f5d360', 'bayan', 21),  -- বান্দার হক
  ('d8d95a49-cd5e-4ebb-8eed-b40b46dd7271', 'bayan', 22),  -- বিবাহ তালাক
  ('f25ba37d-ea87-467b-b1cf-344c66788cb6', 'bayan', 23),  -- ঈদ
  ('27540893-41d0-4140-af40-831becbcee20', 'bayan', 24),  -- সিরাত ও নবী আ. এর যিন্দেগী
  ('7c3a64b1-4096-45ad-9686-ef95942561f9', 'bayan', 25),  -- তারবিয়াতী জলসা
  ('eb0869cf-67c6-46fb-862a-d70547d17c3a', 'bayan', 26),  -- উলামা তোলাবা
  ('bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e', 'bayan', 27),  -- মহিলাদের বিষয়
  ('ab814e54-699a-41e2-8c9c-b24d29b936e3', 'bayan', 28),  -- কুরআন ও তাফসীর
  ('ce1d9d4f-c7e7-455f-832e-6ecf9636be17', 'bayan', 29),  -- হাদীস ও শরাহ
  ('9d094c4c-01d6-465c-a99a-cff2b903c54b', 'bayan', 30),  -- ইবাদাত
  ('98886a03-6f7b-42e2-9386-76701d3589a0', 'bayan', 31)  -- রাজনীতি ও ব্রিটিশের আগ্রাশন  [no old position — appended]
ON CONFLICT DO NOTHING;

-- Malfuzat — 36 categories, 13 appended with no old position
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('0c520e46-2a7c-4a3f-8680-c00aa7e1fb47', 'malfuzat', 1),  -- ঈমান আক্বাইদ
  ('41a519f8-a3f7-44dd-a8f6-94160d91cfbe', 'malfuzat', 2),  -- গুনাহ শিরক কুফর ও বিদ‘আত
  ('935dd29a-e118-4365-ad79-f670d0bb0547', 'malfuzat', 3),  -- অপসংস্কৃতি ও বাতিল সম্প্রদায়
  ('99e51a9a-5736-471a-b39e-e8e161a13a4f', 'malfuzat', 4),  -- নামায
  ('a1cebb34-4f8d-4ab4-b67f-39a9ad219dc7', 'malfuzat', 5),  -- রোযা ও ইতিকাফ
  ('7df5cda9-fb69-40cc-b249-3b986499e4c1', 'malfuzat', 6),  -- যাকাত
  ('9f8dc41f-b2f6-4cde-8fb7-934ea16845da', 'malfuzat', 7),  -- হাজ্জ
  ('632c4bba-ffa9-4ef0-b5ce-cea7b8091655', 'malfuzat', 8),  -- সুন্নত
  ('ab814e54-699a-41e2-8c9c-b24d29b936e3', 'malfuzat', 9),  -- কুরআন ও তাফসীর
  ('7e6c3374-44e8-4044-81f1-ffb3adc080e6', 'malfuzat', 10),  -- দু‘আ দুরূদ ও যিকির
  ('d8d95a49-cd5e-4ebb-8eed-b40b46dd7271', 'malfuzat', 11),  -- বিবাহ তালাক
  ('8281e28b-5c7e-46f5-af8e-64145d5a3909', 'malfuzat', 12),  -- লেনদেন কামাই রোজগার
  ('a8b55714-78d3-403e-97ad-120794f5d360', 'malfuzat', 13),  -- বান্দার হক
  ('415f3a00-b916-4b39-9ab5-606711d1dd61', 'malfuzat', 14),  -- আত্মশুদ্ধি
  ('1dd3bbb3-7528-4fd3-be91-65f9999da2a5', 'malfuzat', 15),  -- দাওয়াত ও তাবলীগ
  ('092bf0db-657c-44ab-91c7-0fcca8ffa251', 'malfuzat', 16),  -- দীন শিক্ষা ও তাহ্ক্বীক্বাত
  ('eb0869cf-67c6-46fb-862a-d70547d17c3a', 'malfuzat', 17),  -- উলামা তোলাবা
  ('bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e', 'malfuzat', 18),  -- মহিলাদের বিষয়
  ('348fce34-2964-4f4d-b728-2b3e24f2a735', 'malfuzat', 19),  -- স্বাস্থ্যবিধি
  ('98886a03-6f7b-42e2-9386-76701d3589a0', 'malfuzat', 20),  -- রাজনীতি ও ব্রিটিশের আগ্রাশন
  ('d25162ff-4359-4ce3-8ba3-b645a5212485', 'malfuzat', 21),  -- অন্যান্য ঘটনা ইতিহাস
  ('d2477f7c-ee5f-4e05-9891-8318695b049c', 'malfuzat', 22),  -- বিবিধ
  ('cd124146-0c35-416e-8ee2-beb2bbe51977', 'malfuzat', 23),  -- আল্লাহওয়ালা
  ('4b211683-b7a8-4a63-810d-4114792624e6', 'malfuzat', 24),  -- গুনাহ ও বিদ‘আত  [no old position — appended]
  ('ce1d9d4f-c7e7-455f-832e-6ecf9636be17', 'malfuzat', 25),  -- হাদীস ও শরাহ  [no old position — appended]
  ('bcc0e436-347d-4d74-871a-1f2683415e93', 'malfuzat', 26),  -- আল্লাহওয়ালাগণের যিন্দেগী  [no old position — appended]
  ('2fb02bbb-cb84-4fda-a654-87f9f999011f', 'malfuzat', 27),  -- মালফুযাত ও মাওয়ায়েজ  [no old position — appended]
  ('6f34df24-1165-4045-9693-6c92cb62b1d3', 'malfuzat', 28),  -- কুফর শিরক বিদ‘আত কুসংস্কার অপসংস্কৃতি  [no old position — appended]
  ('56462255-22cd-4596-944f-1990d5ab2d7c', 'malfuzat', 29),  -- বাতিল সম্প্রদায় ও গোমরাহ দল  [no old position — appended]
  ('14841a9c-d42d-4cc4-8d08-992e4f8b012d', 'malfuzat', 30),  -- মাযহাব ও তাকলীদ  [no old position — appended]
  ('8c99d6b3-32af-4839-8066-d895e090abcd', 'malfuzat', 31),  -- সামাজিকতা আচার ব্যবহার ও রাজনীতি  [no old position — appended]
  ('2c317e8b-5795-45fe-9959-7903807e2901', 'malfuzat', 32),  -- পোশাক পরিচ্ছদ  [no old position — appended]
  ('f30fadfb-59d4-4279-93f9-5478c07f62e2', 'malfuzat', 33),  -- সকাল-সন্ধ্যার আমল ও দু‘আ প্রসঙ্গ  [no old position — appended]
  ('322906ce-fb9b-4939-9a26-6b2008588c5f', 'malfuzat', 34),  -- জুম‘আ  [no old position — appended]
  ('ffb30b1d-b602-43d0-9568-8f0199e42266', 'malfuzat', 35),  -- তালীম  [no old position — appended]
  ('762c638c-59d1-40d5-aa62-3f540485412c', 'malfuzat', 36)  -- বর্তমান প্রেক্ষাপট রাজনীতি ও সামাজিকতা  [no old position — appended]
ON CONFLICT DO NOTHING;

-- Masail — 40 categories, 1 appended with no old position
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('e7fe159a-841f-45b3-8610-16d65cac1fc8', 'masail', 1),  -- অসুস্থ ও মাযূর
  ('54ab56e1-1511-4aba-a954-8aa3b43a5053', 'masail', 2),  -- আযান ইক্বামাত ও ইমাম মু‘আযযিন
  ('f02d7a3d-2c12-43c7-b783-41d2d2712f8e', 'masail', 3),  -- আইন ও দন্ডবিধি
  ('eb0869cf-67c6-46fb-862a-d70547d17c3a', 'masail', 4),  -- উলামা তোলাবা
  ('0c520e46-2a7c-4a3f-8680-c00aa7e1fb47', 'masail', 5),  -- ঈমান আক্বাইদ
  ('7298ae1b-5c91-496d-a1f9-ec394eacfbfe', 'masail', 6),  -- উযূ গোসল ইস্তেন্জা তায়াম্মুম ও মাসাহ
  ('ab814e54-699a-41e2-8c9c-b24d29b936e3', 'masail', 7),  -- কুরআন ও তাফসীর
  ('b8517d83-157e-4284-b8b4-194357782f03', 'masail', 8),  -- কাফন দাফন জানাযা কবর যিয়ারত
  ('6f34df24-1165-4045-9693-6c92cb62b1d3', 'masail', 9),  -- কুফর শিরক বিদ‘আত কুসংস্কার অপসংস্কৃতি
  ('b12816b1-6ff8-446b-a648-133522668f80', 'masail', 10),  -- কাযা কাফফারা ও ফিদয়া
  ('0869feb4-b194-440b-8820-b4bb8133f892', 'masail', 11),  -- কাপড় পানি যায়গা পাক নাপাক
  ('2bee4ace-77f9-4e9c-ad92-f123f28189ed', 'masail', 12),  -- কুরবানী ও আক্বিকা
  ('554bd019-391e-4c2b-b89d-9b7f81604c67', 'masail', 13),  -- জিহাদ
  ('415f3a00-b916-4b39-9ab5-606711d1dd61', 'masail', 14),  -- আত্মশুদ্ধি
  ('142ab098-ee79-4166-9f77-881572541d2f', 'masail', 15),  -- তাবীজ-কবচ তাদবীর
  ('7e6c3374-44e8-4044-81f1-ffb3adc080e6', 'masail', 16),  -- দু‘আ দুরূদ ও যিকির
  ('1dd3bbb3-7528-4fd3-be91-65f9999da2a5', 'masail', 17),  -- দাওয়াত ও তাবলীগ
  ('99e51a9a-5736-471a-b39e-e8e161a13a4f', 'masail', 18),  -- নামায
  ('70bc592e-e893-4f02-a8b0-1b730c31f4b2', 'masail', 19),  -- মেয়েদের বিষয়
  ('e89cd609-e815-499e-b218-7317571e8deb', 'masail', 20),  -- ফাতাওয়া প্রসঙ্গ ও এর গুরুত্ব
  ('56462255-22cd-4596-944f-1990d5ab2d7c', 'masail', 21),  -- বাতিল সম্প্রদায় ও গোমরাহ দল
  ('a8b55714-78d3-403e-97ad-120794f5d360', 'masail', 22),  -- বান্দার হক
  ('8281e28b-5c7e-46f5-af8e-64145d5a3909', 'masail', 23),  -- লেনদেন কামাই রোজগার
  ('d8d95a49-cd5e-4ebb-8eed-b40b46dd7271', 'masail', 24),  -- বিবাহ তালাক
  ('a8cb3636-b4c3-44f0-893f-764624a9717e', 'masail', 25),  -- মসজিদ মাদরাসা
  ('bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e', 'masail', 26),  -- মহিলাদের বিষয়
  ('fb5cce80-ad7a-4308-8b38-01b39fb865b1', 'masail', 27),  -- মিরাস বন্টন
  ('14841a9c-d42d-4cc4-8d08-992e4f8b012d', 'masail', 28),  -- মাযহাব ও তাকলীদ
  ('7df5cda9-fb69-40cc-b249-3b986499e4c1', 'masail', 29),  -- যাকাত
  ('a1cebb34-4f8d-4ab4-b67f-39a9ad219dc7', 'masail', 30),  -- রোযা ও ইতিকাফ
  ('632c4bba-ffa9-4ef0-b5ce-cea7b8091655', 'masail', 31),  -- সুন্নত
  ('8c99d6b3-32af-4839-8066-d895e090abcd', 'masail', 32),  -- সামাজিকতা আচার ব্যবহার ও রাজনীতি
  ('27540893-41d0-4140-af40-831becbcee20', 'masail', 33),  -- সিরাত ও নবী আ. এর যিন্দেগী
  ('358f08a8-48d4-4973-828d-20abb5846052', 'masail', 34),  -- সফর ও মুসাফির
  ('5f28d8e3-fe16-4119-a444-32261124531a', 'masail', 35),  -- হায়েয নেফায
  ('9f8dc41f-b2f6-4cde-8fb7-934ea16845da', 'masail', 36),  -- হাজ্জ
  ('d2477f7c-ee5f-4e05-9891-8318695b049c', 'masail', 37),  -- বিবিধ
  ('2c317e8b-5795-45fe-9959-7903807e2901', 'masail', 38),  -- পোশাক পরিচ্ছদ
  ('cba918e3-8641-4490-a23f-4ceb6167917e', 'masail', 39),  -- কসম ও মান্নত
  ('3f05871a-e590-494b-a935-46b8a02f8a37', 'masail', 40)  -- মাসআলা-মাসাইল  [no old position — appended]
ON CONFLICT DO NOTHING;

-- Dua — 17 categories, all positions recovered
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('9106ef07-f6e9-4e62-aaab-952878235240', 'dua', 1),  -- ভূমিকা
  ('4cc9eb94-ef78-4e4e-ba66-b20cb3808c94', 'dua', 2),  -- মুনাজাত
  ('ab727c56-70e1-485c-8ee2-1efc9bc85ef1', 'dua', 3),  -- ফরজ নামাযের পর দু‘আ ও আমল
  ('f30fadfb-59d4-4279-93f9-5478c07f62e2', 'dua', 4),  -- সকাল-সন্ধ্যার আমল ও দু‘আ প্রসঙ্গ
  ('17488281-6145-4472-b9af-e98710d54ee2', 'dua', 5),  -- পবিত্র কুরআন থেকে সংগৃহীত দু‘আ প্রসঙ্গ
  ('7553f6c3-d60f-48e2-bd65-1da836490508', 'dua', 6),  -- হাদীস শরীফ থেকে সংগৃহীত দু‘আ
  ('137809eb-516a-4a24-b88b-2e35e9f87144', 'dua', 7),  -- উলামায়েকেরাম থেকে সংগৃহিত দু‘আ
  ('f7d46fbe-e7a7-4a26-a94d-d5dc709070d5', 'dua', 8),  -- ইস্তিগফার প্রসঙ্গ
  ('309c71ab-edce-40de-9982-0ba80a2fba5a', 'dua', 9),  -- দুরূদ শরীফ প্রসঙ্গ
  ('8b2ec538-38ff-4021-8451-6f2c624532d0', 'dua', 10),  -- বিভিন্ন স্থান ও সময়ের দু‘আ
  ('80cd7aa1-997f-4f65-994b-eab4041d303d', 'dua', 11),  -- কিছু নফল নামাযের প্রসঙ্গ
  ('cdf3315f-59ec-4411-aae8-7cba6bc75542', 'dua', 12),  -- অত্যন্ত ফযীলতপূর্ণ বিশেষ কিছু আমল
  ('11a3a29e-28c4-445e-9990-a14494137efa', 'dua', 13),  -- মানযিল (জ্বীন-শয়তান থেকে বাঁচার আমাল)
  ('1281d11b-e44c-4fab-9185-134c81027564', 'dua', 14),  -- রুকইয়াহ ও তাদবীর বা জ্বীনের চিকিৎসা
  ('290a76d8-ac43-4de3-afd1-d6441a678e0c', 'dua', 15),  -- মুনাজাতে মাকবূল
  ('c46e0652-3b61-4cf8-8084-5072cc1d42e7', 'dua', 16),  -- নামাযের ভিতরের দু‘আ ও তাসবীহ
  ('40f4b334-82a1-4297-91c7-211d1bd4c22f', 'dua', 17)  -- ফযিলতপূর্ণ সূরা ও আয়াত
ON CONFLICT DO NOTHING;

-- Article — 19 categories, 2 appended with no old position
INSERT INTO category_modules ("CategoryId", "Module", "Position") VALUES
  ('0c520e46-2a7c-4a3f-8680-c00aa7e1fb47', 'article', 1),  -- ঈমান আক্বাইদ
  ('935dd29a-e118-4365-ad79-f670d0bb0547', 'article', 2),  -- অপসংস্কৃতি ও বাতিল সম্প্রদায়
  ('99e51a9a-5736-471a-b39e-e8e161a13a4f', 'article', 3),  -- নামায
  ('a1cebb34-4f8d-4ab4-b67f-39a9ad219dc7', 'article', 4),  -- রোযা ও ইতিকাফ
  ('7df5cda9-fb69-40cc-b249-3b986499e4c1', 'article', 5),  -- যাকাত
  ('9f8dc41f-b2f6-4cde-8fb7-934ea16845da', 'article', 6),  -- হাজ্জ
  ('e3f83eab-4805-46b0-9dea-35ea0f24c6de', 'article', 7),  -- বারো মাসের করণীয় ও বর্জনীয়
  ('7e6c3374-44e8-4044-81f1-ffb3adc080e6', 'article', 8),  -- দু‘আ দুরূদ ও যিকির
  ('8281e28b-5c7e-46f5-af8e-64145d5a3909', 'article', 9),  -- লেনদেন কামাই রোজগার
  ('70adec75-1602-49c3-8908-2e6f93f14f07', 'article', 10),  -- হালাল-হারাম ও জায়োয নাজায়েয
  ('a8b55714-78d3-403e-97ad-120794f5d360', 'article', 11),  -- বান্দার হক
  ('415f3a00-b916-4b39-9ab5-606711d1dd61', 'article', 12),  -- আত্মশুদ্ধি
  ('1dd3bbb3-7528-4fd3-be91-65f9999da2a5', 'article', 13),  -- দাওয়াত ও তাবলীগ
  ('bdd3bd11-b2f5-4f67-bba2-04a1c3b60c1e', 'article', 14),  -- মহিলাদের বিষয়
  ('762c638c-59d1-40d5-aa62-3f540485412c', 'article', 15),  -- বর্তমান প্রেক্ষাপট রাজনীতি ও সামাজিকতা
  ('a8cb3636-b4c3-44f0-893f-764624a9717e', 'article', 16),  -- মসজিদ মাদরাসা
  ('d2477f7c-ee5f-4e05-9891-8318695b049c', 'article', 17),  -- বিবিধ
  ('d8d95a49-cd5e-4ebb-8eed-b40b46dd7271', 'article', 18),  -- বিবাহ তালাক  [no old position — appended]
  ('d25162ff-4359-4ce3-8ba3-b645a5212485', 'article', 19)  -- অন্যান্য ঘটনা ইতিহাস  [no old position — appended]
ON CONFLICT DO NOTHING;

DO $$
DECLARE n int;
BEGIN
  SELECT count(*) INTO n FROM category_modules;
  IF n <> 174 THEN RAISE EXCEPTION 'expected 174 category_modules rows, found %', n; END IF;

  -- every category that holds content in a module must have a membership row
  SELECT count(*) INTO n FROM (
    SELECT 'book' m, "CategoriesId" g FROM book_categories
    UNION SELECT 'bayan', "CategoriesId" FROM bayan_categories
    UNION SELECT 'dua', "CategoriesId" FROM dua_categories
    UNION SELECT 'malfuzat', "CategoriesId" FROM malfuzat_categories
    UNION SELECT 'masail', "CategoriesId" FROM masail_categories
    UNION SELECT 'article', "CategoriesId" FROM article_categories) x
  WHERE NOT EXISTS (
    SELECT 1 FROM category_modules cm WHERE cm."CategoryId" = x.g AND cm."Module" = x.m);
  IF n <> 0 THEN RAISE EXCEPTION '% content-bearing pairs have no membership row', n; END IF;

  RAISE NOTICE 'category_modules seeded: 174 rows';
END $$;

COMMIT;

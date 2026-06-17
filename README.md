# Islami Jindegi — .NET API

A production REST API powering [Islami Jindegi](https://islamijindegi.com), a Bengali-language Islamic content platform. The backend serves a Next.js web app and a cross-platform Flutter mobile app, delivering Quranic text, audio bayans, books, scholarly writings, prayer times, and a Hijri calendar engine with per-country moon-sighting overrides.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Framework | ASP.NET Core (Web API controllers) |
| ORM | Entity Framework Core 10 + Npgsql |
| Database | PostgreSQL |
| Object Storage | AWS S3-compatible (Tigris) |
| Deployment | Fly.io (Mumbai region) |
| API Docs | ASP.NET Core OpenAPI (built-in) |

---

## Architecture

The codebase follows a clean layered architecture with a clear separation of concerns:

```
Controllers/        ← Thin HTTP layer. Binds parameters, calls service, returns response.
Services/
  Interfaces/       ← 15 service interfaces for dependency injection
  *Service.cs       ← All business logic and database queries (EF Core)
Data/
  AppDbContext.cs   ← EF Core DbContext with all entity configurations
Models/             ← EF Core entity classes (16 models)
DTOs/               ← Request/response records (21 DTO files)
Mappers.cs          ← Centralized static mapping from model → DTO
Migrations/         ← EF Core migration history (8 migrations)
Commands/           ← CLI data migration commands (Ruby on Rails → .NET)
Services/
  StorageService.cs ← S3-compatible file upload/delete/presigned URL abstraction
```

### Key Design Decisions

**Interface-based DI** — Every domain service exposes an interface in `Services/Interfaces/`. Controllers depend only on the interface, making the codebase testable and swappable without touching any routing code.

**Centralized mapping** — A single `Mappers.cs` static class holds all model-to-DTO projection methods. This eliminates duplication across 15 services and makes response shapes easy to audit in one place.

**Lean controllers** — Each controller is a thin routing shell: it binds HTTP parameters, calls one service method, and converts the result to the appropriate HTTP response. Zero business logic or database access lives in controllers.

**EF Core split queries** — Deep object graphs (e.g. Books with Authors, Categories, Chapters, and SubChapters) use `.AsSplitQuery()` to avoid the N+1 and cartesian explosion problems on eager loads.

---

## Domain Model

The platform manages the following 16 domain entities:

| Entity | Description |
|---|---|
| **Author** | Islamic scholars and authors (shared across Books, Bayans, Malfuzat, Masail, Articles) |
| **Category** | Self-referential tree (parent/child) — shared tag taxonomy for all content types |
| **Book** | Islamic books with cover image, PDF, many-to-many Authors and Categories |
| **Chapter** | Book chapters, hierarchical via SubChapters |
| **SubChapter** | Nested sub-sections within a chapter (self-referential parent) |
| **Bayan** | Audio lectures/sermons with Author, Categories, and audio URL |
| **Malfuzat** | Scholarly sayings/discourses with optional audio |
| **Masail** | Q&A fatwa entries (question + answer) with optional audio |
| **Dua** | Supplications with Arabic text, audio, and categories |
| **Article** | Long-form written articles with Author and Categories |
| **News** | News items with publishable body content |
| **Madrasah** | Islamic schools with structured info sections and photo galleries |
| **NamazTime** | Prayer time descriptions (masail + fazail for each of the 5 prayers) |
| **HijriMonthSighting** | Per-country moon sighting overrides for the Hijri calendar engine |
| **QuranAyah** | Arabic text for all 6,236 ayahs across 114 surahs |
| **QuranTranslation** | Multi-translator ayah translations (Bengali, English) |
| **QuranWord** | Per-word Arabic and Bengali breakdown for every ayah |
| **Media** | Uploaded file registry (images, audio, PDFs) tracked in the database |

---

## API Reference

All endpoints return JSON. List endpoints are paginated with `?page=1&pageSize=N`.

### Authors — `/api/authors`
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/authors` | Paginated list with optional `?search=` |
| `GET` | `/api/authors/{id}` | Single author |
| `POST` | `/api/authors` | Create author |
| `PUT` | `/api/authors/{id}` | Update author |
| `DELETE` | `/api/authors/{id}` | Delete author |

### Categories — `/api/categories`
Returns a tree structure (parent with nested children). Used as a shared taxonomy across all content modules.

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/categories` | All root categories with nested children |
| `GET` | `/api/categories/{id}` | Single category with children |
| `POST` | `/api/categories` | Create category (supports `parentId` for nesting) |
| `PUT` | `/api/categories/{id}` | Update |
| `DELETE` | `/api/categories/{id}` | Delete |

### Books — `/api/books`
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/books` | Paginated list — filters: `search`, `authorId`, `categoryId`, `published` |
| `GET` | `/api/books/authors` | Authors that have books, with book counts |
| `GET` | `/api/books/categories` | Categories that have books, with counts |
| `GET` | `/api/books/{id}` | Book detail with full chapter tree |
| `POST` | `/api/books` | Create book |
| `PUT` | `/api/books/{id}` | Update book |
| `DELETE` | `/api/books/{id}` | Delete book |

### Chapters & SubChapters
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/chapters` | Paginated flat list — filters: `bookId`, `search` |
| `GET` | `/api/chapters/{id}` | Chapter with sub-chapters |
| `GET` | `/api/books/{bookId}/chapters` | All chapters for a book |
| `POST` | `/api/books/{bookId}/chapters` | Create chapter under a book |
| `PUT` | `/api/chapters/{id}` | Update chapter |
| `DELETE` | `/api/chapters/{id}` | Delete chapter |
| `GET` | `/api/subchapters` | Paginated flat list — filters: `bookId`, `search` |
| `GET` | `/api/subchapters/{id}` | SubChapter detail |
| `POST` | `/api/subchapters` | Create sub-chapter (flat, with `chapterId` in body) |
| `POST` | `/api/chapters/{chapterId}/subchapters` | Create sub-chapter nested under chapter |
| `PUT` | `/api/subchapters/{id}` | Update |
| `DELETE` | `/api/subchapters/{id}` | Delete |

### Bayans — `/api/bayan`
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/bayan` | Paginated list — filters: `search`, `authorId`, `categoryId`, `published`, `sort` (`date` or `position`) |
| `GET` | `/api/bayan/authors` | Authors with bayan counts |
| `GET` | `/api/bayan/categories` | Categories with bayan counts |
| `GET` | `/api/bayan/{id}` | Single bayan with audio URL |
| `POST` | `/api/bayan` | Create |
| `PUT` | `/api/bayan/{id}` | Update |
| `DELETE` | `/api/bayan/{id}` | Delete |

### Malfuzat — `/api/malfuzat`
Same shape as Bayan with an additional `hasAudio` filter.

### Masail — `/api/masail`
Same shape as Malfuzat. Each entry has a `question` and optional `answer` field in addition to standard metadata.

### Dua — `/api/dua`
Category-only filtering (no author). Includes `hasAudio` filter.

### Articles — `/api/articles`
Same shape as Bayan with Author + Category relationships.

### News — `/api/news`
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/news` | Paginated list — filters: `search`, `published` |
| `GET` | `/api/news/{id}` | Full article body |
| `POST` / `PUT` / `DELETE` | `/api/news/{id}` | CRUD |

### Madrasahs — `/api/madrasahs`
Each madrasah has a list of `Infos` (structured key/value sections) and `Photos` (gallery). Both are replaced wholesale on update.

### Namaz Times — `/api/namaz-times`
Each of the 5 daily prayers has a `masail` (juristic rulings) and `fazail` (virtues) text body.

### Hijri Calendar — `/api/hijri`
A custom Hijri calendar engine built on the Umm al-Qura calendar with a three-tier resolution strategy:

1. **DB override** — explicit moon sighting records stored per country/year/month
2. **Country default offset** — Bangladesh, India, Pakistan, Australia default to +1 day from Saudi Arabia
3. **Saudi base** — exact Umm al-Qura calculation

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/hijri/date` | Resolve a Gregorian date to Hijri — params: `country-code`, `date` (yyyy-MM-dd) |
| `GET` | `/api/hijri/month` | Full Hijri month info — params: `country-code`, `hijri-year`, `hijri-month` |
| `GET` | `/api/hijri/sightings` | CRUD list of moon sighting overrides |
| `POST` | `/api/hijri/sightings` | Add override |
| `PUT` | `/api/hijri/sightings/{id}` | Update override |
| `DELETE` | `/api/hijri/sightings/{id}` | Remove override |

Example response for `GET /api/hijri/date?country-code=BD`:
```json
{
  "data": {
    "hijriYear": 1447,
    "hijriMonth": 12,
    "hijriDay": 19,
    "monthLength": 29,
    "monthNameEn": "Dhu al-Hijjah",
    "monthNameAr": "ذو الحجة",
    "monthNameBn": "জিলহজ"
  },
  "meta": {
    "countryCode": "BD",
    "resolvedBy": "default_offset",
    "offsetDays": 1,
    "saGregorianStartDate": "2025-05-27",
    "gregorianStartDate": "2025-05-28",
    "nextGregorianStartDate": "2025-06-26"
  }
}
```

### Quran — `/api/quran`
| Method | Path | Description |
|---|---|---|
| `GET` | `/api/quran/surahs` | Metadata for all 114 surahs (Arabic name, Bengali name, English name, transliteration, total ayahs, revelation type, para number) |
| `GET` | `/api/quran/surahs/{number}/ayahs` | Full surah — Arabic text, translations, and per-word breakdown; filter by `?translator=` |
| `GET` | `/api/quran/mushafs` | List of available Mushaf editions with CDN configuration |
| `GET` | `/api/quran/mushafs/{editionId}` | Single Mushaf config (page dimensions, total pages, CDN base URL, ayah boxes URL) |
| `POST` | `/api/quran/mushaf-url` | Presigned S3 URL for a Mushaf zip download (mobile app offline cache) |
| `POST` | `/api/quran/tafsir-url` | Presigned URL for a Tafsir JSON file |
| `POST` | `/api/quran/db-url` | Presigned URL for an SQLite database asset |
| `POST` | `/api/quran/sura-audio-urls` | Presigned URLs for every ayah audio file in a sura (for a given reciter) |

Available Mushaf editions: Imdadia Hafezi, Hafezi, Colorful Tajweed, Madani (Uthmani print), Nurani, Colorful Hafezi.

Available Tafsirs: Tafsir Taqi Usmani (Bengali), Tafsir Ibn Kathir (Bengali + English), Maariful Quran (Bengali + English).

### Media — `/api/media`
Tracks all uploaded files in the database alongside their S3 storage key.

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/media` | Paginated list — filters: `search`, `type` (`image`, `audio`, `document`) |
| `GET` | `/api/media/{id}` | Single media record |
| `POST` | `/api/media/upload` | Multipart upload (max 200 MB) — supported: JPEG, PNG, WebP, GIF, MP3, M4A, OGG, WAV, PDF |
| `DELETE` | `/api/media/{id}` | Delete from DB and S3 |

### Upload — `/api/upload`
Lightweight upload endpoints for content that doesn't need a media registry entry (e.g. book covers, document URLs set inline during content creation).

| Method | Path | Limit |
|---|---|---|
| `POST` | `/api/upload/image` | 10 MB — JPEG, PNG, WebP, GIF |
| `POST` | `/api/upload/document` | 100 MB — PDF only |

---

## Data Migration

The platform was migrated from a legacy Ruby on Rails + MySQL backend. A dedicated `Commands/` layer handles the one-time data migration:

```bash
# Migrate all content from the old Rails database
dotnet run -- --migrate-data

# Migrate only newly added modules (incremental)
dotnet run -- --migrate-new-modules

# Seed Bangladesh 1447 Hijri sighting data
dotnet run -- --seed-bd-1447
```

The connection to the old database is provided via the `OLD_DATABASE_URL` environment variable, keeping migration code isolated from the production runtime.

---

## Database Schema

EF Core code-first with 8 migrations tracking the full evolution of the schema:

| Migration | Changes |
|---|---|
| `InitialCreate` | Authors, Categories, Books, Chapters, SubChapters, Bayans, Malfuzat, Masail, Duas, Articles |
| `AddSubChapterParent` | Self-referential `ParentSubChapterId` on SubChapter |
| `AddModules` | News, Madrasah (with Infos + Photos) |
| `AddMedia` | Media file registry table |
| `AddMediaDescription` | Description field on Media |
| `AddNewsMadrasahNamazTime` | NamazTime module |
| `AddHijriMonthSighting` | Hijri moon sighting overrides with unique index on `(CountryCode, HijriYear, HijriMonth)` |
| `AddQuranText` | QuranAyah, QuranTranslation, QuranWord with composite unique indexes |

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+
- An S3-compatible object storage bucket (Tigris, AWS S3, MinIO, etc.)

### Environment Variables

| Variable | Description |
|---|---|
| `DATABASE_URL` | PostgreSQL connection URL (`postgres://user:pass@host:5432/db`) |
| `ALLOWED_ORIGINS` | Comma-separated CORS origins (e.g. `http://localhost:3000`) |
| `STORAGE_ACCESS_KEY_ID` | S3 access key |
| `STORAGE_SECRET_ACCESS_KEY` | S3 secret key |
| `STORAGE_ENDPOINT` | S3 endpoint URL (default: `https://fly.storage.tigris.dev`) |
| `STORAGE_BUCKET_NAME` | S3 bucket name (default: `static.islamijindegi.com`) |

### Running Locally

```bash
# Clone the repository
git clone https://github.com/your-username/islami-jindegi-dotnet-api
cd islami-jindegi-dotnet-api

# Set environment variables (or use appsettings.Development.json)
export DATABASE_URL="postgres://postgres:password@localhost:5432/islamijindegi"
export ALLOWED_ORIGINS="http://localhost:3000"

# Run (applies EF migrations automatically on startup)
dotnet run
```

The API will be available at `http://localhost:8080`. In development mode, the OpenAPI schema is served at `/openapi/v1.json`.

---

## Deployment

The API is deployed on **Fly.io** in the `bom` (Mumbai) region to minimize latency for the primary Bangladesh/South Asia user base.

```toml
# fly.toml (key settings)
primary_region = 'bom'
auto_stop_machines = 'off'   # always-on — no cold starts
min_machines_running = 0
memory = '512mb'
```

EF Core migrations run automatically at startup via `db.Database.Migrate()`, making deployments zero-downtime with no manual migration step.

```bash
fly deploy
```

---

## Project Structure

```
islami-jindegi-dotnet-api/
├── Controllers/              # 16 lean API controllers
│   ├── AuthorsController.cs
│   ├── BooksController.cs
│   ├── ChaptersController.cs
│   ├── BayanController.cs
│   ├── MalfuzatController.cs
│   ├── MasailController.cs
│   ├── DuaController.cs
│   ├── ArticlesController.cs
│   ├── NewsController.cs
│   ├── MadrasahsController.cs
│   ├── NamazTimesController.cs
│   ├── HijriController.cs
│   ├── MediaController.cs
│   ├── UploadController.cs
│   ├── QuranController.cs
│   └── CategoriesController.cs
├── Services/
│   ├── Interfaces/           # 15 service interfaces
│   │   ├── IAuthorService.cs
│   │   ├── IBookService.cs
│   │   └── ...
│   ├── AuthorService.cs      # 15 service implementations
│   ├── BookService.cs
│   ├── HijriService.cs       # Hijri calendar engine
│   ├── QuranService.cs       # Quran text + asset service
│   ├── StorageService.cs     # S3 abstraction
│   └── ...
├── Data/
│   └── AppDbContext.cs       # EF Core context + model config
├── Models/                   # 16 EF Core entity classes
├── DTOs/                     # 21 request/response record files
├── Mappers.cs                # Centralized model → DTO projection
├── Migrations/               # 8 EF Core migrations
├── Commands/                 # Data migration CLI commands
│   ├── MigrateDataCommand.cs
│   └── SeedBd1447Command.cs
└── Program.cs                # DI registration + app pipeline
```

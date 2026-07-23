# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 / ASP.NET Core REST API powering [Islami Jindegi](https://islamijindegi.com), a Bengali-language Islamic content platform (Quranic text, audio bayans, books, scholarly writings, prayer times, and a custom Hijri calendar engine). It serves a Next.js web app and a Flutter mobile app. Migrated from a legacy Ruby on Rails + MySQL backend.

Solo-maintained. Optimize for low ceremony over enforced boundaries.

## Tech Stack

- **.NET 10** / C# 14, nullable + implicit usings enabled
- **ASP.NET Core (MVC controllers)** — not minimal APIs; see Architecture below
- **Entity Framework Core 10 + Npgsql** — PostgreSQL, migrations applied automatically on startup
- **AWS SDK for S3** — object storage (Tigris, S3-compatible), wrapped by `StorageService`
- **ASP.NET Core OpenAPI** (built-in, no Swashbuckle) — schema at `/openapi/v1.json` in Development
- **Fly.io** — deployment (`bom`/Mumbai region)
- No auth/authorization layer yet — the API is open, restricted only by CORS (`ALLOWED_ORIGINS`)
- No test project yet — there is no `dotnet test` target in this repo
- No caching layer — content is read straight from Postgres on every request

## Commands

```bash
# Restore & build
dotnet restore
dotnet build

# Run locally (applies EF migrations automatically on startup)
dotnet run
# -> http://localhost:8080, OpenAPI schema at /openapi/v1.json in Development

# EF Core migrations
dotnet ef migrations add <Name>
dotnet ef database update

# One-time data migration commands (require OLD_DATABASE_URL env var)
dotnet run -- --migrate-data          # full migration from legacy Rails DB
dotnet run -- --migrate-new-modules   # incremental, only newer modules
dotnet run -- --migrate-pages         # migrate legacy static pages
dotnet run -- --seed-bd-1447          # seed Bangladesh 1447 Hijri sighting data
dotnet run -- --import-tafsir         # import tafsir data (requires TAFSIR_DATA_DIR)
dotnet run -- --import-arabic-plain   # import plain-text Arabic ayahs (requires ARABIC_PLAIN_JSON_PATH)

# Deploy (Fly.io, bom/Mumbai region)
fly deploy
```

There is no linter/formatter config beyond default `dotnet build` warnings (no `.editorconfig`, no `Directory.Build.props`).

Local dev DB connection defaults to `appsettings.Development.json` (`localhost:5435`). Production connection comes from the `DATABASE_URL` env var (parsed in `Program.cs`'s `BuildConnectionString`, converting a `postgres://` URL into an Npgsql connection string). `.secrets` holds real credentials for both the new and legacy (`OLD_DATABASE_URL`) databases — never commit values from it elsewhere.

## Architecture

Single-project layered architecture — **not** Clean Architecture, VSA, or DDD. Do not introduce Domain/Application/Infrastructure project splits, MediatR/Mediator, or repository abstractions over EF Core. This is a CRUD-heavy content platform (16 entities, no complex cross-entity invariants); the current structure is intentionally low-ceremony and should stay that way.

```
Controllers/   Thin HTTP layer (MVC). Binds params, calls one service method, returns response. No business logic.
Services/
  Interfaces/  One interface per domain service, used for DI — controllers depend only on these.
  *Service.cs  All business logic and EF Core queries live here, injected with AppDbContext directly (no repository layer).
Data/
  AppDbContext.cs  EF Core DbContext — all entity relationships/indexes configured here (fluent API, no data annotations).
Models/        EF Core entity classes.
DTOs/          Request/response records, grouped one file per domain (e.g. BookDto.cs has SaveBookRequest, BookListItem, BookDetail, etc).
Mappers.cs     Single static class with every model -> DTO projection. Add new mappings here, not inline in services.
Migrations/    EF Core migration history.
Commands/      One-off CLI data-migration commands invoked via `dotnet run -- --flag` (see Program.cs).
```

**Pattern to follow when adding a new domain module** (see `AuthorsController` + `AuthorService` as the minimal reference pair, or `BooksController` + `BookService` for one with relations): Controller -> `IXService` interface -> `XService` implementation -> `Mappers.ToXResponse` -> DTO record in `DTOs/XDto.cs`. Register the new service as `AddScoped<IXService, XService>()` in `Program.cs`'s DI block.

Controller conventions: `[ApiController]` + `[Route("api/x")]`, primary-constructor DI, expression-bodied actions where the body is a single call, `Guid` route constraints (`{id:guid}`), `CreatedAtAction` on create, `NotFound()`/`NoContent()` on missing/deleted.

Service conventions: constructor-injected `AppDbContext db` via primary constructor, `Guid.NewGuid()` generated in the service (not the DB), `CreatedAt`/`UpdatedAt` stamped with `DateTime.UtcNow`, list endpoints return `PagedResult<T>(Data, Total, Page, PageSize)` with `?page=&pageSize=&search=`.

### Key design decisions

- **EF Core split queries**: deep object graphs (Books with Authors/Categories/Chapters/SubChapters) use `.AsSplitQuery()` to avoid cartesian explosion on eager loads.
- **Many-to-many joins** (book-author, book-category, and per-content-type category joins) are configured explicitly in `AppDbContext.OnModelCreating` with named join tables (e.g. `book_authors`).
- **Category** is a self-referential tree (parent/child) shared as a taxonomy across every content type (books, bayans, malfuzat, masail, duas, articles).
- **Storage**: `StorageService` (singleton) wraps an S3-compatible bucket (Tigris) for uploads, deletes, and presigned URLs. Public files live under a fixed `uploads/store/` key prefix mapped to `https://static.islamijindegi.com/uploads/store/`.
- **Hijri calendar engine** (`HijriService`): three-tier resolution per country — (1) explicit DB override in `HijriMonthSighting`, (2) a static per-country default offset from Saudi Arabia (BD/IN/PK/AU = +1 day), (3) exact Umm al-Qura calculation as the Saudi base. See `README.md` for the response shape and `/api/hijri/*` routes.
- **Quran data**: three related tables — `QuranAyah` (Arabic text), `QuranTranslation` (per-translator translations), `QuranWord` (per-word Arabic/Bengali breakdown) — all keyed by `(SurahNumber, AyahNumber[, ...])` rather than FK to a Surah table.
- **Pages**: flat CMS-style static pages with slug lookup (`PageService`/`PagesController`), migrated from the legacy DB via `--migrate-pages`.
- Pagination is a plain `PagedResult<T>(Data, Total, Page, PageSize)` record returned directly by list endpoints (`?page=&pageSize=`). Several list endpoints (authors, categories, and their per-module variants for bayan/masail/malfuzat/dua) additionally support `?search=`.

The full API surface (every route per module) and the DB migration history are documented in `README.md` — read it for endpoint-level detail rather than grepping every controller.

## Coding Standards

- File-scoped namespaces, primary constructors for DI, `var` for obvious types
- PascalCase for public members, suffix async methods with `Async`
- No regions, no comments for obvious code — only comment "why"
- New endpoints/services follow the existing patterns above exactly — do not introduce a new pattern for a single feature

## Anti-patterns

Do NOT:

- Introduce Clean Architecture layers, DDD tactical patterns, MediatR/Mediator, or repository abstractions over EF Core — this project is deliberately a flat layered structure
- Convert controllers to minimal API endpoints — this project uses MVC controllers consistently
- Add a caching layer, message bus, or auth scheme speculatively — none exist here; add only when there's a concrete requirement
- Use `DateTime.Now` — use `DateTime.UtcNow` (matches existing `CreatedAt`/`UpdatedAt` convention)
- Create `new HttpClient()` directly — use `IHttpClientFactory` if an HTTP client is ever needed
- Block with `.Result` or `.Wait()` — await instead
- Return EF Core entities directly from controllers — always map through `Mappers.cs` to a DTO record
- Put business logic or DB queries in controllers — controllers only bind params and call one service method

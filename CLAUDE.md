# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 / ASP.NET Core REST API powering [Islami Jindegi](https://islamijindegi.com), a Bengali-language Islamic content platform (Quranic text, audio bayans, books, scholarly writings, prayer times, and a custom Hijri calendar engine). It serves a Next.js web app and a Flutter mobile app. Migrated from a legacy Ruby on Rails + MySQL backend.

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
dotnet run -- --seed-bd-1447          # seed Bangladesh 1447 Hijri sighting data

# Deploy (Fly.io, bom/Mumbai region)
fly deploy
```

There is no test project in this repo currently, and no linter/formatter config beyond default `dotnet build` warnings.

Local dev DB connection defaults to `appsettings.Development.json` (`localhost:5435`). Production connection comes from the `DATABASE_URL` env var (parsed in `Program.cs`'s `BuildConnectionString`, converting a `postgres://` URL into an Npgsql connection string). `.secrets` holds real credentials for both the new and legacy (`OLD_DATABASE_URL`) databases — never commit values from it elsewhere.

## Architecture

Clean layered architecture, one folder per concern:

```
Controllers/   Thin HTTP layer (MVC). Binds params, calls one service method, returns response. No business logic.
Services/
  Interfaces/  One interface per domain service, used for DI — controllers depend only on these.
  *Service.cs  All business logic and EF Core queries live here.
Data/
  AppDbContext.cs  EF Core DbContext — all entity relationships/indexes configured here (fluent API, no data annotations).
Models/        EF Core entity classes.
DTOs/          Request/response records, grouped one file per domain (e.g. BookDto.cs has SaveBookRequest, BookListItem, BookDetail, etc).
Mappers.cs     Single static class with every model -> DTO projection. Add new mappings here, not inline in services.
Migrations/    EF Core migration history.
Commands/      One-off CLI data-migration commands invoked via `dotnet run -- --flag` (see Program.cs).
```

Pattern to follow when adding a new domain module (see `BooksController` + `BookService` as the reference pair): Controller -> `IXService` interface -> `XService` implementation -> `Mappers.ToXResponse` -> DTO record in `DTOs/XDto.cs`. Register the new service in `Program.cs`'s DI block.

### Key design decisions

- **EF Core split queries**: deep object graphs (Books with Authors/Categories/Chapters/SubChapters) use `.AsSplitQuery()` to avoid cartesian explosion on eager loads.
- **Many-to-many joins** (book-author, book-category, and per-content-type category joins) are configured explicitly in `AppDbContext.OnModelCreating` with named join tables (e.g. `book_authors`).
- **Category** is a self-referential tree (parent/child) shared as a taxonomy across every content type (books, bayans, malfuzat, masail, duas, articles).
- **Storage**: `StorageService` (singleton) wraps an S3-compatible bucket (Tigris) for uploads, deletes, and presigned URLs. Public files live under a fixed `uploads/store/` key prefix mapped to `https://static.islamijindegi.com/uploads/store/`.
- **Hijri calendar engine** (`HijriService`): three-tier resolution per country — (1) explicit DB override in `HijriMonthSighting`, (2) a static per-country default offset from Saudi Arabia (BD/IN/PK/AU = +1 day), (3) exact Umm al-Qura calculation as the Saudi base. See `README.md` for the response shape and `/api/hijri/*` routes.
- **Quran data**: three related tables — `QuranAyah` (Arabic text), `QuranTranslation` (per-translator translations), `QuranWord` (per-word Arabic/Bengali breakdown) — all keyed by `(SurahNumber, AyahNumber[, ...])` rather than FK to a Surah table.
- Pagination is a plain `PagedResult<T>(Data, Total, Page, PageSize)` record returned directly by list endpoints (`?page=&pageSize=`).

The full API surface (every route per module) and the DB migration history are documented in `README.md` — read it for endpoint-level detail rather than grepping every controller.

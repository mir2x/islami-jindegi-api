using System.Globalization;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Endpoints;

public static class HijriEndpoints
{
    // Countries that observe moon sighting 1 day after Saudi Arabia by default
    static readonly Dictionary<string, int> DefaultOffsets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BD"] = 1,
        ["AU"] = 1,
        ["IN"] = 1,
        ["PK"] = 1,
    };

    static readonly Dictionary<int, (string En, string Ar, string Bn)> MonthNames = new()
    {
        [1]  = ("Muharram",       "محرم",          "মুহাররম"),
        [2]  = ("Safar",          "صفر",           "সফর"),
        [3]  = ("Rabi' al-Awwal", "ربيع الأول",    "রবিউল আউয়াল"),
        [4]  = ("Rabi' al-Thani", "ربيع الثاني",   "রবিউস সানি"),
        [5]  = ("Jumada al-Ula",  "جمادى الأولى",  "জুমাদাল উলা"),
        [6]  = ("Jumada al-Thani","جمادى الثانية", "জুমাদাস সানি"),
        [7]  = ("Rajab",          "رجب",           "রজব"),
        [8]  = ("Sha'ban",        "شعبان",         "শাবান"),
        [9]  = ("Ramadan",        "رمضان",         "রমজান"),
        [10] = ("Shawwal",        "شوال",          "শাওয়াল"),
        [11] = ("Dhu al-Qi'dah",  "ذو القعدة",     "জিলক্বদ"),
        [12] = ("Dhu al-Hijjah",  "ذو الحجة",      "জিলহজ"),
    };

    // Umm al-Qura calendar — matches the Python `hijridate` library (Saudi standard)
    static readonly UmAlQuraCalendar Cal = new();

    public static void MapHijriEndpoints(this WebApplication app)
    {
        // ── Public API ────────────────────────────────────────────────────────
        var pub = app.MapGroup("/api/hijri");

        // GET /api/hijri/date?country-code=BD&date=2026-06-10
        pub.MapGet("/date", async (
            AppDbContext db,
            string? countryCode = "BD",
            string? date = null) =>
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return Results.BadRequest(new { error = "country-code is required" });

            DateOnly target;
            if (date is not null)
            {
                if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out target))
                    return Results.BadRequest(new { error = "date must be yyyy-MM-dd" });
            }
            else
            {
                target = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            // Use SA calendar to find the candidate Hijri month
            var saYear  = Cal.GetYear(target.ToDateTime(TimeOnly.MinValue));
            var saMonth = Cal.GetMonth(target.ToDateTime(TimeOnly.MinValue));

            // Check current, previous, and next SA months — country offsets can shift boundaries
            var candidates = new[]
            {
                PrevMonth(saYear, saMonth),
                (saYear, saMonth),
                NextMonth(saYear, saMonth),
            };

            foreach (var (hy, hm) in candidates)
            {
                var (start, source, offset, saStart) = await ResolveStart(db, countryCode, hy, hm);
                var (nextStart, _, _, _)             = await ResolveStart(db, countryCode, NextMonth(hy, hm));

                if (target >= start && target < nextStart)
                {
                    var day         = target.DayNumber - start.DayNumber + 1;
                    var monthLength = nextStart.DayNumber - start.DayNumber;
                    var names       = MonthNames[hm];

                    return Results.Ok(new HijriDateResponse(
                        new HijriDateData(hy, hm, day, monthLength, names.En, names.Ar, names.Bn),
                        new HijriDateMeta(countryCode.ToUpperInvariant(), source, offset, saStart, start, nextStart)));
                }
            }

            return Results.Problem("Could not resolve Hijri date for the given input.");
        });

        // GET /api/hijri/month?country-code=BD&hijri-year=1447&hijri-month=9
        pub.MapGet("/month", async (
            AppDbContext db,
            string? countryCode = "BD",
            int? hijriYear  = null,
            int? hijriMonth = null) =>
        {
            if (string.IsNullOrWhiteSpace(countryCode) || hijriYear is null || hijriMonth is null)
                return Results.BadRequest(new { error = "country-code, hijri-year, and hijri-month are required" });

            if (hijriMonth < 1 || hijriMonth > 12)
                return Results.BadRequest(new { error = "hijri-month must be 1–12" });

            var hy = hijriYear.Value;
            var hm = hijriMonth.Value;

            var (start,     source, offset, saStart) = await ResolveStart(db, countryCode, hy, hm);
            var (nextStart, _,      _,      _)       = await ResolveStart(db, countryCode, NextMonth(hy, hm));

            var monthLength = nextStart.DayNumber - start.DayNumber;
            var names       = MonthNames[hm];

            return Results.Ok(new HijriMonthResponse(
                new HijriMonthData(hy, hm, monthLength, names.En, names.Ar, names.Bn, start, nextStart),
                new HijriMonthMeta(countryCode.ToUpperInvariant(), source, offset, saStart)));
        });

        // ── Admin CRUD ────────────────────────────────────────────────────────
        var admin = app.MapGroup("/api/hijri/sightings");

        admin.MapGet("/", async (
            AppDbContext db,
            int page = 1, int pageSize = 20,
            string? countryCode = null,
            int? hijriYear = null) =>
        {
            var query = db.HijriMonthSightings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(countryCode))
                query = query.Where(s => s.CountryCode == countryCode.ToUpperInvariant());

            if (hijriYear.HasValue)
                query = query.Where(s => s.HijriYear == hijriYear.Value);

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(s => s.HijriYear)
                .ThenBy(s => s.CountryCode)
                .ThenBy(s => s.HijriMonth)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => ToResponse(s))
                .ToListAsync();

            return Results.Ok(new PagedResult<HijriMonthSightingResponse>(data, total, page, pageSize));
        });

        admin.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var s = await db.HijriMonthSightings.FindAsync(id);
            return s is null ? Results.NotFound() : Results.Ok(ToResponse(s));
        });

        admin.MapPost("/", async (CreateHijriSightingRequest req, AppDbContext db) =>
        {
            if (req.HijriMonth < 1 || req.HijriMonth > 12)
                return Results.BadRequest(new { error = "hijriMonth must be 1–12" });

            var exists = await db.HijriMonthSightings.AnyAsync(s =>
                s.CountryCode == req.CountryCode.ToUpperInvariant() &&
                s.HijriYear   == req.HijriYear &&
                s.HijriMonth  == req.HijriMonth);

            if (exists)
                return Results.Conflict(new { error = "A sighting for this country/year/month already exists" });

            var sighting = new HijriMonthSighting
            {
                Id                  = Guid.NewGuid(),
                CountryCode         = req.CountryCode.ToUpperInvariant(),
                HijriYear           = req.HijriYear,
                HijriMonth          = req.HijriMonth,
                GregorianStartDate  = req.GregorianStartDate,
                CreatedAt           = DateTime.UtcNow,
                UpdatedAt           = DateTime.UtcNow,
            };

            db.HijriMonthSightings.Add(sighting);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hijri/sightings/{sighting.Id}", ToResponse(sighting));
        });

        admin.MapPut("/{id:guid}", async (Guid id, UpdateHijriSightingRequest req, AppDbContext db) =>
        {
            if (req.HijriMonth < 1 || req.HijriMonth > 12)
                return Results.BadRequest(new { error = "hijriMonth must be 1–12" });

            var sighting = await db.HijriMonthSightings.FindAsync(id);
            if (sighting is null) return Results.NotFound();

            // Check uniqueness if key fields changed
            var duplicate = await db.HijriMonthSightings.AnyAsync(s =>
                s.Id          != id &&
                s.CountryCode == req.CountryCode.ToUpperInvariant() &&
                s.HijriYear   == req.HijriYear &&
                s.HijriMonth  == req.HijriMonth);

            if (duplicate)
                return Results.Conflict(new { error = "A sighting for this country/year/month already exists" });

            sighting.CountryCode        = req.CountryCode.ToUpperInvariant();
            sighting.HijriYear          = req.HijriYear;
            sighting.HijriMonth         = req.HijriMonth;
            sighting.GregorianStartDate = req.GregorianStartDate;
            sighting.UpdatedAt          = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(sighting));
        });

        admin.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var sighting = await db.HijriMonthSightings.FindAsync(id);
            if (sighting is null) return Results.NotFound();

            db.HijriMonthSightings.Remove(sighting);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static HijriMonthSightingResponse ToResponse(HijriMonthSighting s)
    {
        var names = MonthNames[s.HijriMonth];
        return new HijriMonthSightingResponse(
            s.Id, s.CountryCode, s.HijriYear, s.HijriMonth,
            names.En, names.Ar, names.Bn,
            s.GregorianStartDate, s.CreatedAt, s.UpdatedAt);
    }

    static DateOnly GetSaStart(int hijriYear, int hijriMonth) =>
        DateOnly.FromDateTime(Cal.ToDateTime(hijriYear, hijriMonth, 1, 0, 0, 0, 0));

    static (int Year, int Month) NextMonth(int year, int month) =>
        month == 12 ? (year + 1, 1) : (year, month + 1);

    static (int Year, int Month) PrevMonth(int year, int month) =>
        month == 1 ? (year - 1, 12) : (year, month - 1);

    static (int Year, int Month) NextMonth((int Year, int Month) ym) =>
        NextMonth(ym.Year, ym.Month);

    static async Task<(DateOnly Start, string Source, int OffsetDays, DateOnly SaStart)>
        ResolveStart(AppDbContext db, string countryCode, int hijriYear, int hijriMonth)
    {
        var saStart = GetSaStart(hijriYear, hijriMonth);

        // Tier 1: explicit DB override for this country + month
        var sighting = await db.HijriMonthSightings
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.CountryCode == countryCode.ToUpperInvariant() &&
                s.HijriYear   == hijriYear &&
                s.HijriMonth  == hijriMonth);

        if (sighting is not null)
        {
            var offsetDays = sighting.GregorianStartDate.DayNumber - saStart.DayNumber;
            return (sighting.GregorianStartDate, "override", offsetDays, saStart);
        }

        // Tier 2: country default offset
        if (DefaultOffsets.TryGetValue(countryCode, out var defaultOffset))
            return (saStart.AddDays(defaultOffset), "default_offset", defaultOffset, saStart);

        // Exact SA
        return (saStart, "exact_sa", 0, saStart);
    }

    static async Task<(DateOnly Start, string Source, int OffsetDays, DateOnly SaStart)>
        ResolveStart(AppDbContext db, string countryCode, (int Year, int Month) ym) =>
        await ResolveStart(db, countryCode, ym.Year, ym.Month);
}

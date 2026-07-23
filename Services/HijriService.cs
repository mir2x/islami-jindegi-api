using System.Globalization;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.DTOs;
using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Services;

public class HijriService(AppDbContext db) : IHijriService
{
    static readonly Dictionary<string, int> DefaultOffsets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BD"] = 1, ["AU"] = 1, ["IN"] = 1, ["PK"] = 1,
    };

    static readonly Dictionary<int, (string En, string Ar, string Bn)> MonthNames = new()
    {
        // Bangla spellings are the canonical set shared 1:1 with the Flutter app's
        // l10n (app_bn.arb / hijriMonthsBengali) and the web's HIJRI_MONTHS_BN —
        // keep all three in sync when changing any of them.
        [1]  = ("Muharram",       "محرم",          "মুহাররম"),
        [2]  = ("Safar",          "صفر",           "সফর"),
        [3]  = ("Rabi' al-Awwal", "ربيع الأول",    "রবিউল আউয়াল"),
        [4]  = ("Rabi' al-Thani", "ربيع الثاني",   "রবিউস সানি"),
        [5]  = ("Jumada al-Ula",  "جمادى الأولى",  "জুমাদাল উলা"),
        [6]  = ("Jumada al-Thani","جمادى الثانية", "জুমাদাল উখরা"),
        [7]  = ("Rajab",          "رجب",           "রজব"),
        [8]  = ("Sha'ban",        "شعبان",         "শাবান"),
        [9]  = ("Ramadan",        "رمضان",         "রমাযান"),
        [10] = ("Shawwal",        "شوال",          "শাউয়াল"),
        [11] = ("Dhu al-Qi'dah",  "ذو القعدة",     "যিলক্বদ"),
        [12] = ("Dhu al-Hijjah",  "ذو الحجة",      "যিলহাজ্জ"),
    };

    static readonly UmAlQuraCalendar Cal = new();

    public async Task<(HijriDateResponse? Result, string? Error)> GetDateAsync(string countryCode, string? date)
    {
        DateOnly target;
        if (date is not null)
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out target))
                return (null, "date must be yyyy-MM-dd");
        }
        else
        {
            target = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var saYear  = Cal.GetYear(target.ToDateTime(TimeOnly.MinValue));
        var saMonth = Cal.GetMonth(target.ToDateTime(TimeOnly.MinValue));

        var candidates = new[]
        {
            PrevMonth(saYear, saMonth),
            (saYear, saMonth),
            NextMonth(saYear, saMonth),
        };

        foreach (var (hy, hm) in candidates)
        {
            var (start, source, offset, saStart) = await ResolveStartAsync(countryCode, hy, hm);
            var (nextStart, _, _, _)             = await ResolveStartAsync(countryCode, NextMonth(hy, hm));

            if (target >= start && target < nextStart)
            {
                var day         = target.DayNumber - start.DayNumber + 1;
                var monthLength = nextStart.DayNumber - start.DayNumber;
                var names       = MonthNames[hm];

                return (new HijriDateResponse(
                    new HijriDateData(hy, hm, day, monthLength, names.En, names.Ar, names.Bn),
                    new HijriDateMeta(false, null, countryCode.ToUpperInvariant(), source, offset, saStart, start, nextStart)), null);
            }
        }

        // Unresolvable (e.g. contradictory overrides): not an error — signal fallback so
        // clients switch to their local calculation.
        return (new HijriDateResponse(
            null,
            new HijriDateMeta(true, "no_data", countryCode.ToUpperInvariant(), null, null, null, null, null)), null);
    }

    public async Task<(HijriMonthResponse? Result, string? Error)> GetMonthAsync(string countryCode, int hijriYear, int hijriMonth)
    {
        if (hijriMonth < 1 || hijriMonth > 12)
            return (null, "hijri-month must be 1–12");

        var (start,     source, offset, saStart) = await ResolveStartAsync(countryCode, hijriYear, hijriMonth);
        var (nextStart, _,      _,      _)       = await ResolveStartAsync(countryCode, NextMonth(hijriYear, hijriMonth));

        var monthLength = nextStart.DayNumber - start.DayNumber;

        // Contradictory overrides can make a month non-positive in length; treat as
        // unresolvable rather than serving a nonsensical window.
        if (monthLength <= 0)
        {
            return (new HijriMonthResponse(
                null,
                new HijriMonthMeta(true, "no_data", countryCode.ToUpperInvariant(), null, null, null)), null);
        }

        var names = MonthNames[hijriMonth];

        return (new HijriMonthResponse(
            new HijriMonthData(hijriYear, hijriMonth, monthLength, names.En, names.Ar, names.Bn, start, nextStart),
            new HijriMonthMeta(false, null, countryCode.ToUpperInvariant(), source, offset, saStart)), null);
    }

    public async Task<PagedResult<HijriMonthSightingResponse>> GetSightingsAsync(int page, int pageSize, string? countryCode, int? hijriYear)
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
            .Select(s => ToSightingResponse(s))
            .ToListAsync();

        return new PagedResult<HijriMonthSightingResponse>(data, total, page, pageSize);
    }

    public async Task<HijriMonthSightingResponse?> GetSightingByIdAsync(Guid id)
    {
        var s = await db.HijriMonthSightings.FindAsync(id);
        return s is null ? null : ToSightingResponse(s);
    }

    public async Task<(HijriMonthSightingResponse? Item, string? Error)> CreateSightingAsync(CreateHijriSightingRequest req)
    {
        if (req.HijriMonth < 1 || req.HijriMonth > 12)
            return (null, "hijriMonth must be 1–12");

        var exists = await db.HijriMonthSightings.AnyAsync(s =>
            s.CountryCode == req.CountryCode.ToUpperInvariant() &&
            s.HijriYear   == req.HijriYear &&
            s.HijriMonth  == req.HijriMonth);

        if (exists) return (null, "A sighting for this country/year/month already exists");

        var sighting = new HijriMonthSighting
        {
            Id                 = Guid.NewGuid(),
            CountryCode        = req.CountryCode.ToUpperInvariant(),
            HijriYear          = req.HijriYear,
            HijriMonth         = req.HijriMonth,
            GregorianStartDate = req.GregorianStartDate,
            CreatedAt          = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow,
        };
        db.HijriMonthSightings.Add(sighting);
        await db.SaveChangesAsync();
        return (ToSightingResponse(sighting), null);
    }

    public async Task<(HijriMonthSightingResponse? Item, string? Error)> UpdateSightingAsync(Guid id, UpdateHijriSightingRequest req)
    {
        if (req.HijriMonth < 1 || req.HijriMonth > 12)
            return (null, "hijriMonth must be 1–12");

        var sighting = await db.HijriMonthSightings.FindAsync(id);
        if (sighting is null) return (null, null);

        var duplicate = await db.HijriMonthSightings.AnyAsync(s =>
            s.Id          != id &&
            s.CountryCode == req.CountryCode.ToUpperInvariant() &&
            s.HijriYear   == req.HijriYear &&
            s.HijriMonth  == req.HijriMonth);

        if (duplicate) return (null, "A sighting for this country/year/month already exists");

        sighting.CountryCode        = req.CountryCode.ToUpperInvariant();
        sighting.HijriYear          = req.HijriYear;
        sighting.HijriMonth         = req.HijriMonth;
        sighting.GregorianStartDate = req.GregorianStartDate;
        sighting.UpdatedAt          = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return (ToSightingResponse(sighting), null);
    }

    public async Task<bool> DeleteSightingAsync(Guid id)
    {
        var sighting = await db.HijriMonthSightings.FindAsync(id);
        if (sighting is null) return false;
        db.HijriMonthSightings.Remove(sighting);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static HijriMonthSightingResponse ToSightingResponse(HijriMonthSighting s)
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

    async Task<(DateOnly Start, string Source, int OffsetDays, DateOnly SaStart)>
        ResolveStartAsync(string countryCode, int hijriYear, int hijriMonth)
    {
        var saStart = GetSaStart(hijriYear, hijriMonth);

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

        if (DefaultOffsets.TryGetValue(countryCode, out var defaultOffset))
            return (saStart.AddDays(defaultOffset), "default_offset", defaultOffset, saStart);

        return (saStart, "exact_sa", 0, saStart);
    }

    async Task<(DateOnly Start, string Source, int OffsetDays, DateOnly SaStart)>
        ResolveStartAsync(string countryCode, (int Year, int Month) ym) =>
        await ResolveStartAsync(countryCode, ym.Year, ym.Month);
}

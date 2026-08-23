using System.Text;
using IslamiJindegiApi.Commands;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression();
builder.Services.AddOutputCache(options =>
{
    // The offline-sync responses are whole-corpus payloads, well past the 64MB
    // per-body / 100MB total defaults once several domains are cached at once.
    options.MaximumBodySize = 128L * 1024 * 1024;
    options.SizeLimit = 512L * 1024 * 1024;
});

var connectionString = BuildConnectionString(
    Environment.GetEnvironmentVariable("DATABASE_URL"),
    builder.Configuration.GetConnectionString("DefaultConnection"));

static string BuildConnectionString(string? databaseUrl, string? fallback)
{
    if (databaseUrl is not null)
    {
        var uri = new Uri(databaseUrl.Split('?')[0].Replace("postgres://", "http://"));
        var userInfo = uri.UserInfo.Split(':');
        // Pool and timeout settings live here, not in DATABASE_URL: the query
        // string is stripped above, so anything appended to the secret is lost.
        // A 100-connection default (Npgsql's) lets a single instance queue far
        // more concurrent work than the database can serve, which turns a slow
        // query into thread-pool starvation and takes the whole API down.
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};"
             + $"Username={userInfo[0]};Password={userInfo[1]};"
             + "SSL Mode=Disable;Gss Encryption Mode=Disable;"
             + "Maximum Pool Size=20;Timeout=5;Command Timeout=30";
    }
    return fallback ?? throw new InvalidOperationException("No database connection string configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    // The parameterless overload retries 6 times with delays up to 30s, so a
    // single unreachable database holds every pooled connection for minutes
    // and starves Kestrel. Fail fast instead and let the client retry.
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<ContentSyncNotifier>();

builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IChapterService, ChapterService>();
builder.Services.AddScoped<IBayanService, BayanService>();
builder.Services.AddScoped<IMalfuzatService, MalfuzatService>();
builder.Services.AddScoped<IMasailService, MasailService>();
builder.Services.AddScoped<IDuaService, DuaService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IMadrasahService, MadrasahService>();
builder.Services.AddScoped<INamazTimeService, NamazTimeService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IHijriService, HijriService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IQuranService, QuranService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var adminJwtSecret = Environment.GetEnvironmentVariable("ADMIN_JWT_SECRET")
    ?? throw new InvalidOperationException("ADMIN_JWT_SECRET not set.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminJwtSecret))
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "http://localhost:3000,http://localhost:3001")
    .Split(',');

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseMiddleware<ResponseCacheInvalidationMiddleware>();
app.MapControllers();

// Liveness only — deliberately does not touch the database. Fly restarts the
// machine when this stops answering, which is the recovery path for a process
// wedged by thread-pool starvation or GC pressure. A DB-backed check would
// keep the machine getting killed during a database incident instead.
app.MapGet("/health", () => Results.Ok("ok")).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (args.Contains("--migrate-data") || args.Contains("--migrate-new-modules") || args.Contains("--migrate-pages"))
    {
        var oldUrl = Environment.GetEnvironmentVariable("OLD_DATABASE_URL")
            ?? throw new InvalidOperationException("OLD_DATABASE_URL not set.");
        var oldConnStr = BuildConnectionString(oldUrl, null);

        if (args.Contains("--migrate-new-modules"))
            await MigrateDataCommand.RunNewModulesAsync(oldConnStr, db);
        else if (args.Contains("--migrate-pages"))
            await MigrateDataCommand.RunPagesAsync(oldConnStr, db);
        else
        {
            var fromStep = args.FirstOrDefault(a => a.StartsWith("--from="))?["--from=".Length..];
            await MigrateDataCommand.RunAsync(oldConnStr, db, fromStep);
        }

        return;
    }

    if (args.Contains("--seed-bd-1447"))
    {
        await SeedBd1447Command.RunAsync(db);
        return;
    }

    if (args.Contains("--migrate-hijri-sightings"))
    {
        var source = Environment.GetEnvironmentVariable("HIJRI_DATABASE_URL")
            ?? throw new InvalidOperationException("HIJRI_DATABASE_URL not set.");
        await MigrateHijriSightingsCommand.RunAsync(source, db);
        return;
    }

    if (args.Contains("--import-tafsir"))
    {
        var dataDir = Environment.GetEnvironmentVariable("TAFSIR_DATA_DIR")
            ?? throw new InvalidOperationException("TAFSIR_DATA_DIR not set.");
        await ImportTafsirCommand.RunAsync(db, dataDir);
        return;
    }

    if (args.Contains("--import-arabic-plain"))
    {
        var jsonPath = Environment.GetEnvironmentVariable("ARABIC_PLAIN_JSON_PATH")
            ?? throw new InvalidOperationException("ARABIC_PLAIN_JSON_PATH not set.");
        await ImportArabicPlainTextCommand.RunAsync(db, jsonPath);
        return;
    }

    if (args.Contains("--backfill-offline-availability"))
    {
        var dataDir = Environment.GetEnvironmentVariable("OFFLINE_DATA_DIR") ?? "offline_data";
        await BackfillOfflineAvailabilityCommand.RunAsync(db, dataDir);
        return;
    }

    if (args.Contains("--reset-books-offline-availability"))
    {
        await ResetBooksOfflineAvailabilityCommand.RunAsync(db);
        return;
    }

    if (args.Contains("--set-offline-availability-defaults"))
    {
        await SetOfflineAvailabilityDefaultsCommand.RunAsync(db);
        return;
    }
}

app.Run();

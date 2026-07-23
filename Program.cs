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

var connectionString = BuildConnectionString(
    Environment.GetEnvironmentVariable("DATABASE_URL"),
    builder.Configuration.GetConnectionString("DefaultConnection"));

static string BuildConnectionString(string? databaseUrl, string? fallback)
{
    if (databaseUrl is not null)
    {
        var uri = new Uri(databaseUrl.Split('?')[0].Replace("postgres://", "http://"));
        var userInfo = uri.UserInfo.Split(':');
        return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable;Gss Encryption Mode=Disable";
    }
    return fallback ?? throw new InvalidOperationException("No database connection string configured.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddSingleton<StorageService>();

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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

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
            await MigrateDataCommand.RunAsync(oldConnStr, db);

        return;
    }

    if (args.Contains("--seed-bd-1447"))
    {
        await SeedBd1447Command.RunAsync(db);
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
}

app.Run();

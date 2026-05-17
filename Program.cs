using IslamiJindegiApi.Commands;
using IslamiJindegiApi.Data;
using IslamiJindegiApi.Endpoints;
using IslamiJindegiApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<StorageService>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (args.Contains("--migrate-data"))
    {
        var oldUrl = Environment.GetEnvironmentVariable("OLD_DATABASE_URL")
            ?? throw new InvalidOperationException("OLD_DATABASE_URL not set.");
        var oldConnStr = BuildConnectionString(oldUrl, null);
        await MigrateDataCommand.RunAsync(oldConnStr, db);
        return;
    }
}

app.MapAuthorEndpoints();
app.MapCategoryEndpoints();
app.MapBookEndpoints();
app.MapChapterEndpoints();
app.MapUploadEndpoints();

app.Run();

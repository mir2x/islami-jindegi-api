using IslamiJindegiApi.Models;
using Microsoft.EntityFrameworkCore;

namespace IslamiJindegiApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Media> Medias => Set<Media>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<SubChapter> SubChapters => Set<SubChapter>();
    public DbSet<Malfuzat> Malfuzats => Set<Malfuzat>();
    public DbSet<Masail> Masails => Set<Masail>();
    public DbSet<Dua> Duas => Set<Dua>();
    public DbSet<Bayan> Bayans => Set<Bayan>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<News> News => Set<News>();
    public DbSet<Madrasah> Madrasahs => Set<Madrasah>();
    public DbSet<MadrasahInfo> MadrasahInfos => Set<MadrasahInfo>();
    public DbSet<MadrasahPhoto> MadrasahPhotos => Set<MadrasahPhoto>();
    public DbSet<NamazTime> NamazTimes => Set<NamazTime>();
    public DbSet<HijriMonthSighting> HijriMonthSightings => Set<HijriMonthSighting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .UsingEntity(j => j.ToTable("book_authors"));

        modelBuilder.Entity<Book>()
            .HasMany(b => b.Categories)
            .WithMany(c => c.Books)
            .UsingEntity(j => j.ToTable("book_categories"));

        modelBuilder.Entity<Malfuzat>()
            .HasMany(m => m.Categories)
            .WithMany(c => c.Malfuzats)
            .UsingEntity(j => j.ToTable("malfuzat_categories"));

        modelBuilder.Entity<Masail>()
            .HasMany(m => m.Categories)
            .WithMany(c => c.Masails)
            .UsingEntity(j => j.ToTable("masail_categories"));

        modelBuilder.Entity<Dua>()
            .HasMany(d => d.Categories)
            .WithMany(c => c.Duas)
            .UsingEntity(j => j.ToTable("dua_categories"));

        modelBuilder.Entity<Bayan>()
            .HasMany(b => b.Categories)
            .WithMany(c => c.Bayans)
            .UsingEntity(j => j.ToTable("bayan_categories"));

        modelBuilder.Entity<Article>()
            .HasMany(a => a.Categories)
            .WithMany(c => c.Articles)
            .UsingEntity(j => j.ToTable("article_categories"));

        modelBuilder.Entity<HijriMonthSighting>()
            .HasIndex(h => new { h.CountryCode, h.HijriYear, h.HijriMonth })
            .IsUnique();

        modelBuilder.Entity<HijriMonthSighting>()
            .HasIndex(h => new { h.CountryCode, h.GregorianStartDate });
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class ReadQueryPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX \"IX_quran_ayahs_ArabicTextPlain_trgm\" ON quran_ayahs USING gin (\"ArabicTextPlain\" gin_trgm_ops) WHERE \"ArabicTextPlain\" IS NOT NULL;");
            migrationBuilder.Sql("CREATE INDEX \"IX_quran_translations_TranslationText_trgm\" ON quran_translations USING gin (\"TranslationText\" gin_trgm_ops);");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_IsOfflineAvailable_UpdatedAt",
                table: "Pages",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Masails_IsOfflineAvailable_UpdatedAt",
                table: "Masails",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Malfuzats_IsOfflineAvailable_UpdatedAt",
                table: "Malfuzats",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Madrasahs_IsOfflineAvailable_UpdatedAt",
                table: "Madrasahs",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Duas_IsOfflineAvailable_UpdatedAt",
                table: "Duas",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_IsOfflineAvailable_UpdatedAt",
                table: "Books",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bayans_IsOfflineAvailable_UpdatedAt",
                table: "Bayans",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_IsOfflineAvailable_UpdatedAt",
                table: "Articles",
                columns: new[] { "IsOfflineAvailable", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_quran_translations_TranslationText_trgm\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_quran_ayahs_ArabicTextPlain_trgm\";");

            migrationBuilder.DropIndex(
                name: "IX_Pages_IsOfflineAvailable_UpdatedAt",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Masails_IsOfflineAvailable_UpdatedAt",
                table: "Masails");

            migrationBuilder.DropIndex(
                name: "IX_Malfuzats_IsOfflineAvailable_UpdatedAt",
                table: "Malfuzats");

            migrationBuilder.DropIndex(
                name: "IX_Madrasahs_IsOfflineAvailable_UpdatedAt",
                table: "Madrasahs");

            migrationBuilder.DropIndex(
                name: "IX_Duas_IsOfflineAvailable_UpdatedAt",
                table: "Duas");

            migrationBuilder.DropIndex(
                name: "IX_Books_IsOfflineAvailable_UpdatedAt",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Bayans_IsOfflineAvailable_UpdatedAt",
                table: "Bayans");

            migrationBuilder.DropIndex(
                name: "IX_Articles_IsOfflineAvailable_UpdatedAt",
                table: "Articles");
        }
    }
}

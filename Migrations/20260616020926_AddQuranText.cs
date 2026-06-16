using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_ayahs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurahNumber = table.Column<int>(type: "integer", nullable: false),
                    AyahNumber = table.Column<int>(type: "integer", nullable: false),
                    ArabicText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_ayahs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quran_translations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurahNumber = table.Column<int>(type: "integer", nullable: false),
                    AyahNumber = table.Column<int>(type: "integer", nullable: false),
                    TranslatorName = table.Column<string>(type: "text", nullable: false),
                    TranslationText = table.Column<string>(type: "text", nullable: false),
                    QuranAyahId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quran_translations_quran_ayahs_QuranAyahId",
                        column: x => x.QuranAyahId,
                        principalTable: "quran_ayahs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "quran_words",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurahNumber = table.Column<int>(type: "integer", nullable: false),
                    AyahNumber = table.Column<int>(type: "integer", nullable: false),
                    WordId = table.Column<int>(type: "integer", nullable: false),
                    ArabicWord = table.Column<string>(type: "text", nullable: false),
                    BengaliWord = table.Column<string>(type: "text", nullable: false),
                    QuranAyahId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quran_words_quran_ayahs_QuranAyahId",
                        column: x => x.QuranAyahId,
                        principalTable: "quran_ayahs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_SurahNumber_AyahNumber",
                table: "quran_ayahs",
                columns: new[] { "SurahNumber", "AyahNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_translations_QuranAyahId",
                table: "quran_translations",
                column: "QuranAyahId");

            migrationBuilder.CreateIndex(
                name: "IX_quran_translations_SurahNumber_AyahNumber_TranslatorName",
                table: "quran_translations",
                columns: new[] { "SurahNumber", "AyahNumber", "TranslatorName" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_QuranAyahId",
                table: "quran_words",
                column: "QuranAyahId");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_SurahNumber_AyahNumber_WordId",
                table: "quran_words",
                columns: new[] { "SurahNumber", "AyahNumber", "WordId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_translations");

            migrationBuilder.DropTable(
                name: "quran_words");

            migrationBuilder.DropTable(
                name: "quran_ayahs");
        }
    }
}

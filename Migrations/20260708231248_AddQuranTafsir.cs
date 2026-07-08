using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranTafsir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_tafsirs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SurahNumber = table.Column<int>(type: "integer", nullable: false),
                    AyahNumber = table.Column<int>(type: "integer", nullable: false),
                    TafsirId = table.Column<string>(type: "text", nullable: false),
                    TafsirText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsirs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsirs_SurahNumber_AyahNumber_TafsirId",
                table: "quran_tafsirs",
                columns: new[] { "SurahNumber", "AyahNumber", "TafsirId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_tafsirs");
        }
    }
}

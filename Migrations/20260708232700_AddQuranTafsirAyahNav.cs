using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranTafsirAyahNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuranAyahId",
                table: "quran_tafsirs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsirs_QuranAyahId",
                table: "quran_tafsirs",
                column: "QuranAyahId");

            migrationBuilder.AddForeignKey(
                name: "FK_quran_tafsirs_quran_ayahs_QuranAyahId",
                table: "quran_tafsirs",
                column: "QuranAyahId",
                principalTable: "quran_ayahs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_tafsirs_quran_ayahs_QuranAyahId",
                table: "quran_tafsirs");

            migrationBuilder.DropIndex(
                name: "IX_quran_tafsirs_QuranAyahId",
                table: "quran_tafsirs");

            migrationBuilder.DropColumn(
                name: "QuranAyahId",
                table: "quran_tafsirs");
        }
    }
}

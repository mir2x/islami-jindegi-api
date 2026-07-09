using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranAyahArabicPlain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArabicTextPlain",
                table: "quran_ayahs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArabicTextPlain",
                table: "quran_ayahs");
        }
    }
}

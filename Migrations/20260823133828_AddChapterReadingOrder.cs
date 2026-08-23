using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterReadingOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chapters_BookId",
                table: "Chapters");

            migrationBuilder.AddColumn<int>(
                name: "ReadingOrder",
                table: "SubChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReadingOrder",
                table: "Chapters",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubChapters_ReadingOrder",
                table: "SubChapters",
                column: "ReadingOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId_ReadingOrder",
                table: "Chapters",
                columns: new[] { "BookId", "ReadingOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubChapters_ReadingOrder",
                table: "SubChapters");

            migrationBuilder.DropIndex(
                name: "IX_Chapters_BookId_ReadingOrder",
                table: "Chapters");

            migrationBuilder.DropColumn(
                name: "ReadingOrder",
                table: "SubChapters");

            migrationBuilder.DropColumn(
                name: "ReadingOrder",
                table: "Chapters");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_BookId",
                table: "Chapters",
                column: "BookId");
        }
    }
}

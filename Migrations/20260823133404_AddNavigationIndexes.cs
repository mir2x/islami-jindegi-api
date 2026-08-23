using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_News_Published_Position_Id",
                table: "News",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Masails_Published_Position_Id",
                table: "Masails",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Malfuzats_Published_Position_Id",
                table: "Malfuzats",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Madrasahs_Position_Id",
                table: "Madrasahs",
                columns: new[] { "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Duas_Published_Position_Id",
                table: "Duas",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Published_Position_Id",
                table: "Books",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Bayans_Published_Position_Id",
                table: "Bayans",
                columns: new[] { "Published", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Published_Position_Id",
                table: "Articles",
                columns: new[] { "Published", "Position", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_News_Published_Position_Id",
                table: "News");

            migrationBuilder.DropIndex(
                name: "IX_Masails_Published_Position_Id",
                table: "Masails");

            migrationBuilder.DropIndex(
                name: "IX_Malfuzats_Published_Position_Id",
                table: "Malfuzats");

            migrationBuilder.DropIndex(
                name: "IX_Madrasahs_Position_Id",
                table: "Madrasahs");

            migrationBuilder.DropIndex(
                name: "IX_Duas_Published_Position_Id",
                table: "Duas");

            migrationBuilder.DropIndex(
                name: "IX_Books_Published_Position_Id",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Bayans_Published_Position_Id",
                table: "Bayans");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Published_Position_Id",
                table: "Articles");
        }
    }
}

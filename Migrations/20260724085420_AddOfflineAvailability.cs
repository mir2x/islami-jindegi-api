using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Pages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Masails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Malfuzats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Madrasahs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Duas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Books",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Bayans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfflineAvailable",
                table: "Articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Masails");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Malfuzats");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Madrasahs");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Duas");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Bayans");

            migrationBuilder.DropColumn(
                name: "IsOfflineAvailable",
                table: "Articles");
        }
    }
}

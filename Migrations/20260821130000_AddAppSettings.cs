using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    public partial class AddAppSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AskQuestion = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOfflineQuran = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AppSettings", x => x.Id));

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "AskQuestion", "DisplayOfflineQuran", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    new Guid("9f5e6d11-8098-4a2c-8a8d-1c31fc6a0c65"), true, false,
                    new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppSettings");
        }
    }
}

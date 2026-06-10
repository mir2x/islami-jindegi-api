using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddHijriMonthSighting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HijriMonthSightings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    HijriYear = table.Column<int>(type: "integer", nullable: false),
                    HijriMonth = table.Column<int>(type: "integer", nullable: false),
                    GregorianStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HijriMonthSightings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HijriMonthSightings_CountryCode_GregorianStartDate",
                table: "HijriMonthSightings",
                columns: new[] { "CountryCode", "GregorianStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HijriMonthSightings_CountryCode_HijriYear_HijriMonth",
                table: "HijriMonthSightings",
                columns: new[] { "CountryCode", "HijriYear", "HijriMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HijriMonthSightings");
        }
    }
}

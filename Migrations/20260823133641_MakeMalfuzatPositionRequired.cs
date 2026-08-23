using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeMalfuzatPositionRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Malfuzats" SET "Position" = source.rn
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "Position" NULLS LAST, "CreatedAt")::int AS rn
                    FROM "Malfuzats"
                ) AS source
                WHERE "Malfuzats"."Id" = source."Id" AND "Malfuzats"."Position" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Position",
                table: "Malfuzats",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Position",
                table: "Malfuzats",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

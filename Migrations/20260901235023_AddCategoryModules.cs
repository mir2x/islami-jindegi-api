using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_modules",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_modules", x => new { x.CategoryId, x.Module });
                    table.ForeignKey(
                        name: "FK_category_modules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_modules_Module_Position",
                table: "category_modules",
                columns: new[] { "Module", "Position" });

            migrationBuilder.Sql("""
                ALTER TABLE category_modules ADD CONSTRAINT ck_category_modules_module
                CHECK ("Module" IN ('book','bayan','malfuzat','masail','dua','article'));
                """);

            // Categories used to be consolidated by RENAMING one onto another, which does not
            // move content -- the result was seven duplicate titles with the content split
            // between the copies. Two of those differed only by apostrophe (U+2019 vs ASCII)
            // or Bengali Unicode composition, so a plain unique index would not have caught
            // them. Normalising both makes the constraint match how a reader sees the title.
            //
            // This FAILS if any duplicate remains, which is deliberate: the data scripts in
            // Scripts/01..04 must run against the database before this migration is deployed.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ix_categories_title_normalized
                ON "Categories" (normalize(translate(btrim("Title"), U&'\2018\2019', ''''''), NFC));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS ix_categories_title_normalized;""");

            migrationBuilder.DropTable(
                name: "category_modules");
        }
    }
}

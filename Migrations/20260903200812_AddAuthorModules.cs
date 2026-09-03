using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bayans_Authors_AuthorId",
                table: "Bayans");

            migrationBuilder.DropForeignKey(
                name: "FK_Malfuzats_Authors_AuthorId",
                table: "Malfuzats");

            migrationBuilder.CreateTable(
                name: "author_modules",
                columns: table => new
                {
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_author_modules", x => new { x.AuthorId, x.Module });
                    table.ForeignKey(
                        name: "FK_author_modules_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_author_modules_Module_Position",
                table: "author_modules",
                columns: new[] { "Module", "Position" });

            migrationBuilder.Sql("""
                ALTER TABLE author_modules ADD CONSTRAINT ck_author_modules_module
                CHECK ("Module" IN ('book','bayan','malfuzat','masail','article'));
                """);

            // Authors were being consolidated by RENAMING one onto another, which does not move
            // content -- "মুফতী মনসূরুল হক সাহেব" now exists twice, byte for byte, with the bayan and
            // article on one row and the book, malfuzat and masail on the other. The other two
            // pairs differ only by an invisible zero-width joiner or by Bengali Unicode
            // composition, so a plain unique index would not have caught them. Folding the
            // zero-width characters and the curly apostrophes, then normalising, makes the
            // constraint match how a reader sees the name.
            //
            // This FAILS if any duplicate remains, which is deliberate: Scripts/06 must run
            // against the database before this migration is deployed.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX ix_authors_name_normalized
                ON "Authors" (normalize(translate(btrim("Name"), U&'\2018\2019\200B\200C\200D\FEFF', ''''''), NFC));
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Bayans_Authors_AuthorId",
                table: "Bayans",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Malfuzats_Authors_AuthorId",
                table: "Malfuzats",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bayans_Authors_AuthorId",
                table: "Bayans");

            migrationBuilder.DropForeignKey(
                name: "FK_Malfuzats_Authors_AuthorId",
                table: "Malfuzats");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS ix_authors_name_normalized;""");

            migrationBuilder.DropTable(
                name: "author_modules");

            migrationBuilder.AddForeignKey(
                name: "FK_Bayans_Authors_AuthorId",
                table: "Bayans",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Malfuzats_Authors_AuthorId",
                table: "Malfuzats",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

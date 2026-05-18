using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSubChapterParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "SubChapters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentSubChapterId",
                table: "SubChapters",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubChapters_ParentSubChapterId",
                table: "SubChapters",
                column: "ParentSubChapterId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubChapters_SubChapters_ParentSubChapterId",
                table: "SubChapters",
                column: "ParentSubChapterId",
                principalTable: "SubChapters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubChapters_SubChapters_ParentSubChapterId",
                table: "SubChapters");

            migrationBuilder.DropIndex(
                name: "IX_SubChapters_ParentSubChapterId",
                table: "SubChapters");

            migrationBuilder.DropColumn(
                name: "ParentSubChapterId",
                table: "SubChapters");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "SubChapters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}

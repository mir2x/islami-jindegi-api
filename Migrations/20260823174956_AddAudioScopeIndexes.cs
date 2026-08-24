using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IslamiJindegiApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioScopeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Masails_Published_HasAudio_Position_Id",
                table: "Masails",
                columns: new[] { "Published", "HasAudio", "Position", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Malfuzats_Published_HasAudio_Position_Id",
                table: "Malfuzats",
                columns: new[] { "Published", "HasAudio", "Position", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Masails_Published_HasAudio_Position_Id",
                table: "Masails");

            migrationBuilder.DropIndex(
                name: "IX_Malfuzats_Published_HasAudio_Position_Id",
                table: "Malfuzats");
        }
    }
}

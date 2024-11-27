using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VDR5.Migrations
{
    /// <inheritdoc />
    public partial class Files_FullPath_AddUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Files",
                newName: "FullPath");

            migrationBuilder.CreateIndex(
                name: "IX_Files_FullPath",
                table: "Files",
                column: "FullPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_FullPath",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "FullPath",
                table: "Files",
                newName: "Path");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VDR5.Migrations
{
    /// <inheritdoc />
    public partial class Files_IsDeleted_Column_Add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Files",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Files");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Files",
                newName: "UploadedAt");
        }
    }
}

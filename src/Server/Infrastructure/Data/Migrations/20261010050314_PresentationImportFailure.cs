using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PresentationImportFailure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Presentations");

            migrationBuilder.AddColumn<string>(
                name: "Failure",
                table: "Presentations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Failure",
                table: "Presentations");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Presentations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

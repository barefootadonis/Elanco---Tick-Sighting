using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elanco___Tick_Sighting.Migrations
{
    /// <inheritdoc />
    public partial class AddSightingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "TickSightings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LatinName",
                table: "TickSightings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Species",
                table: "TickSightings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "TickSightings");

            migrationBuilder.DropColumn(
                name: "LatinName",
                table: "TickSightings");

            migrationBuilder.DropColumn(
                name: "Species",
                table: "TickSightings");
        }
    }
}

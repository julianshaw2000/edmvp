using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSmelterSourcingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacilityLocation",
                table: "rmap_smelters",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MineralType",
                table: "rmap_smelters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "SourcingCountries",
                table: "rmap_smelters",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacilityLocation",
                table: "rmap_smelters");

            migrationBuilder.DropColumn(
                name: "MineralType",
                table: "rmap_smelters");

            migrationBuilder.DropColumn(
                name: "SourcingCountries",
                table: "rmap_smelters");
        }
    }
}

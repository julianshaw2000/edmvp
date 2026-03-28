using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRegulations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Regulations",
                table: "tenants",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Regulations",
                table: "tenants");
        }
    }
}

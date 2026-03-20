using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParentBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleVersion",
                table: "compliance_checks",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "1.0.0-pilot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuleVersion",
                table: "compliance_checks");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFormSdTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_sd_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicabilityStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RuleSetVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EngineVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reasoning = table.Column<string>(type: "jsonb", nullable: true),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_sd_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "form_sd_filing_cycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportingYear = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_sd_filing_cycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "form_sd_packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportingYear = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleSetVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlatformVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceJson = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_sd_packages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_form_sd_assessments_BatchId_TenantId",
                table: "form_sd_assessments",
                columns: new[] { "BatchId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_form_sd_assessments_SupersedesId",
                table: "form_sd_assessments",
                column: "SupersedesId");

            migrationBuilder.CreateIndex(
                name: "IX_form_sd_filing_cycles_TenantId_ReportingYear",
                table: "form_sd_filing_cycles",
                columns: new[] { "TenantId", "ReportingYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_form_sd_packages_TenantId_ReportingYear",
                table: "form_sd_packages",
                columns: new[] { "TenantId", "ReportingYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_sd_assessments");

            migrationBuilder.DropTable(
                name: "form_sd_filing_cycles");

            migrationBuilder.DropTable(
                name: "form_sd_packages");
        }
    }
}

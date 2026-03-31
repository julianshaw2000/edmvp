using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCmrtImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cmrt_imports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeclarationCompany = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ReportingYear = table.Column<int>(type: "integer", nullable: true),
                    RowsParsed = table.Column<int>(type: "integer", nullable: false),
                    RowsMatched = table.Column<int>(type: "integer", nullable: false),
                    RowsUnmatched = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    ImportedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cmrt_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cmrt_imports_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cmrt_imports_users_ImportedBy",
                        column: x => x.ImportedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_smelter_associations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SmelterId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CmrtImportId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MetalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_smelter_associations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tenant_smelter_associations_cmrt_imports_CmrtImportId",
                        column: x => x.CmrtImportId,
                        principalTable: "cmrt_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tenant_smelter_associations_rmap_smelters_SmelterId",
                        column: x => x.SmelterId,
                        principalTable: "rmap_smelters",
                        principalColumn: "SmelterId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_smelter_associations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cmrt_imports_ImportedBy",
                table: "cmrt_imports",
                column: "ImportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_cmrt_imports_TenantId",
                table: "cmrt_imports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_smelter_associations_CmrtImportId",
                table: "tenant_smelter_associations",
                column: "CmrtImportId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_smelter_associations_SmelterId",
                table: "tenant_smelter_associations",
                column: "SmelterId");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_smelter_associations_TenantId_SmelterId",
                table: "tenant_smelter_associations",
                columns: new[] { "TenantId", "SmelterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_smelter_associations");

            migrationBuilder.DropTable(
                name: "cmrt_imports");
        }
    }
}

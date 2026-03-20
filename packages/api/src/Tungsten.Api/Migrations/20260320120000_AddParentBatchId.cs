using System;
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
            migrationBuilder.AddColumn<Guid>(
                name: "ParentBatchId",
                table: "batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_batches_ParentBatchId",
                table: "batches",
                column: "ParentBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_batches_batches_ParentBatchId",
                table: "batches",
                column: "ParentBatchId",
                principalTable: "batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batches_batches_ParentBatchId",
                table: "batches");

            migrationBuilder.DropIndex(
                name: "IX_batches_ParentBatchId",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "ParentBatchId",
                table: "batches");
        }
    }
}

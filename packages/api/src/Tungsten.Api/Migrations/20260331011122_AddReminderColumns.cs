using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNudgedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "batches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNudgedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "batches");
        }
    }
}

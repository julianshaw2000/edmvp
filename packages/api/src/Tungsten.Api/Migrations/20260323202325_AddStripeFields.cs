using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_stripe_customer",
                table: "tenants",
                column: "StripeCustomerId",
                unique: true,
                filter: "\"StripeCustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_stripe_subscription",
                table: "tenants",
                column: "StripeSubscriptionId",
                unique: true,
                filter: "\"StripeSubscriptionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenants_stripe_customer",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "ix_tenants_stripe_subscription",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "tenants");
        }
    }
}

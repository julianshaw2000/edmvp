using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tungsten.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuth0SubToEntraOid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Auth0Sub",
                table: "users",
                newName: "entra_oid");

            migrationBuilder.RenameIndex(
                name: "IX_users_Auth0Sub",
                table: "users",
                newName: "IX_users_entra_oid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "entra_oid",
                table: "users",
                newName: "Auth0Sub");

            migrationBuilder.RenameIndex(
                name: "IX_users_entra_oid",
                table: "users",
                newName: "IX_users_Auth0Sub");
        }
    }
}

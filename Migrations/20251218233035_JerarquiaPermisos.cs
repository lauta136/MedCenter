using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class JerarquiaPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PermisoPadreId",
                table: "permisos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_permisos_PermisoPadreId",
                table: "permisos",
                column: "PermisoPadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_permisos_permisos_PermisoPadreId",
                table: "permisos",
                column: "PermisoPadreId",
                principalTable: "permisos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_permisos_permisos_PermisoPadreId",
                table: "permisos");

            migrationBuilder.DropIndex(
                name: "IX_permisos_PermisoPadreId",
                table: "permisos");

            migrationBuilder.DropColumn(
                name: "PermisoPadreId",
                table: "permisos");
        }
    }
}

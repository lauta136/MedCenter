using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class CascadeForManualGroupsDefinitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "grupo_id_fkey",
                table: "permiso_grupo");

            migrationBuilder.DropForeignKey(
                name: "grupo_id_fkey",
                table: "persona_grupo");

            migrationBuilder.AddForeignKey(
                name: "grupo_id_fkey",
                table: "permiso_grupo",
                column: "grupo_id",
                principalTable: "grupos_permisos_personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "grupo_id_fkey",
                table: "persona_grupo",
                column: "grupo_id",
                principalTable: "grupos_permisos_personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "grupo_id_fkey",
                table: "permiso_grupo");

            migrationBuilder.DropForeignKey(
                name: "grupo_id_fkey",
                table: "persona_grupo");

            migrationBuilder.AddForeignKey(
                name: "grupo_id_fkey",
                table: "permiso_grupo",
                column: "grupo_id",
                principalTable: "grupos_permisos_personas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "grupo_id_fkey",
                table: "persona_grupo",
                column: "grupo_id",
                principalTable: "grupos_permisos_personas",
                principalColumn: "id");
        }
    }
}

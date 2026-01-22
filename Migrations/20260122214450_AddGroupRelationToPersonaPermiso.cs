using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupRelationToPersonaPermiso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_persona_permiso_persona_id",
                table: "persona_permiso");

            migrationBuilder.AddColumn<int>(
                name: "GrupoId",
                table: "persona_permiso",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origen",
                table: "persona_permiso",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_persona_permiso_GrupoId",
                table: "persona_permiso",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_persona_permiso_persona_id_permiso_id_GrupoId",
                table: "persona_permiso",
                columns: new[] { "persona_id", "permiso_id", "GrupoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_persona_permiso_grupos_permisos_personas_GrupoId",
                table: "persona_permiso",
                column: "GrupoId",
                principalTable: "grupos_permisos_personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persona_permiso_grupos_permisos_personas_GrupoId",
                table: "persona_permiso");

            migrationBuilder.DropIndex(
                name: "IX_persona_permiso_GrupoId",
                table: "persona_permiso");

            migrationBuilder.DropIndex(
                name: "IX_persona_permiso_persona_id_permiso_id_GrupoId",
                table: "persona_permiso");

            migrationBuilder.DropColumn(
                name: "GrupoId",
                table: "persona_permiso");

            migrationBuilder.DropColumn(
                name: "origen",
                table: "persona_permiso");

            migrationBuilder.CreateIndex(
                name: "IX_persona_permiso_persona_id",
                table: "persona_permiso",
                column: "persona_id");
        }
    }
}

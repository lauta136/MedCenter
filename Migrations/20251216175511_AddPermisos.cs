using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admins_personas_id",
                table: "admins");

            migrationBuilder.CreateTable(
                name: "permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    recurso = table.Column<string>(type: "text", nullable: false),
                    accion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("permiso_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "persona_permiso",
                columns: table => new
                {
                    permiso_id = table.Column<int>(type: "integer", nullable: false),
                    persona_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("persona_permiso_pkey", x => new { x.permiso_id, x.persona_id });
                    table.ForeignKey(
                        name: "persona_permiso_permiso_fkey",
                        column: x => x.permiso_id,
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "persona_permiso_persona_fkey",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rol_permiso",
                columns: table => new
                {
                    rol_nombre = table.Column<int>(type: "integer", nullable: false),
                    permiso_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rol_permiso_pkey", x => new { x.permiso_id, x.rol_nombre });
                    table.ForeignKey(
                        name: "rol_permiso_permiso_fkey",
                        column: x => x.permiso_id,
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Nombre",
                table: "permisos",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persona_permiso_persona_id",
                table: "persona_permiso",
                column: "persona_id");

            migrationBuilder.AddForeignKey(
                name: "admins_id_fkey",
                table: "admins",
                column: "id",
                principalTable: "personas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "admins_id_fkey",
                table: "admins");

            migrationBuilder.DropTable(
                name: "persona_permiso");

            migrationBuilder.DropTable(
                name: "rol_permiso");

            migrationBuilder.DropTable(
                name: "permisos");

            migrationBuilder.AddForeignKey(
                name: "FK_admins_personas_id",
                table: "admins",
                column: "id",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class GruposPermisosPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupos_permisos_personas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    fecha_creacion = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupos_permisos_personas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permiso_grupo",
                columns: table => new
                {
                    permiso_id = table.Column<int>(type: "integer", nullable: false),
                    grupo_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("permiso_grupo_fkey", x => new { x.grupo_id, x.permiso_id });
                    table.ForeignKey(
                        name: "grupo_id_fkey",
                        column: x => x.grupo_id,
                        principalTable: "grupos_permisos_personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "permiso_id_fkey",
                        column: x => x.permiso_id,
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "persona_grupo",
                columns: table => new
                {
                    persona_id = table.Column<int>(type: "integer", nullable: false),
                    grupo_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("persona_grupo_pkey", x => new { x.persona_id, x.grupo_id });
                    table.ForeignKey(
                        name: "grupo_id_fkey",
                        column: x => x.grupo_id,
                        principalTable: "grupos_permisos_personas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "persona_id_fkey",
                        column: x => x.persona_id,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grupos_permisos_personas_nombre",
                table: "grupos_permisos_personas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permiso_grupo_permiso_id",
                table: "permiso_grupo",
                column: "permiso_id");

            migrationBuilder.CreateIndex(
                name: "IX_persona_grupo_grupo_id",
                table: "persona_grupo",
                column: "grupo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permiso_grupo");

            migrationBuilder.DropTable(
                name: "persona_grupo");

            migrationBuilder.DropTable(
                name: "grupos_permisos_personas");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AgregarObrasSociales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "turnos_paciente_id_fkey",
                table: "turnos");

            migrationBuilder.AddColumn<bool>(
                name: "es_particular",
                table: "turnos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "pacienteobrasocial_id",
                table: "turnos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "obrassociales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sigla = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("obra_social_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medico_obrasocial",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    medico_id = table.Column<int>(type: "integer", nullable: false),
                    obrasocial_id = table.Column<int>(type: "integer", nullable: false),
                    fecha_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medico_obrasocial", x => x.id);
                    table.ForeignKey(
                        name: "medico_obrasocial_medico_fkey",
                        column: x => x.medico_id,
                        principalTable: "medicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "medico_obrasocial_obrasocial_fkey",
                        column: x => x.obrasocial_id,
                        principalTable: "obrassociales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "paciente_obrasocial",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    fecha_afiliacion = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_baja = table.Column<DateOnly>(type: "date", nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    paciente_id = table.Column<int>(type: "integer", nullable: false),
                    obrasocial_id = table.Column<int>(type: "integer", nullable: false),
                    numeroAfiliado = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    plan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paciente_obrasocial", x => x.id);
                    table.ForeignKey(
                        name: "paciente_obrasocial_obrasocial_fkey",
                        column: x => x.obrasocial_id,
                        principalTable: "obrassociales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "paciente_obrasocial_paciente_fkey",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_turnos_pacienteobrasocial_id",
                table: "turnos",
                column: "pacienteobrasocial_id");

            migrationBuilder.CreateIndex(
                name: "IX_medico_obrasocial_medico_id_obrasocial_id_activo",
                table: "medico_obrasocial",
                columns: new[] { "medico_id", "obrasocial_id", "activo" },
                unique: true,
                filter: "activo = true");

            migrationBuilder.CreateIndex(
                name: "IX_medico_obrasocial_obrasocial_id",
                table: "medico_obrasocial",
                column: "obrasocial_id");

            migrationBuilder.CreateIndex(
                name: "IX_paciente_obrasocial_obrasocial_id",
                table: "paciente_obrasocial",
                column: "obrasocial_id");

            migrationBuilder.CreateIndex(
                name: "IX_paciente_obrasocial_paciente_id_obrasocial_id_activa",
                table: "paciente_obrasocial",
                columns: new[] { "paciente_id", "obrasocial_id", "activa" },
                unique: true,
                filter: "activa = TRUE");

            migrationBuilder.AddForeignKey(
                name: "turno_paciente_obrasocial_fkey",
                table: "turnos",
                column: "pacienteobrasocial_id",
                principalTable: "paciente_obrasocial",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "turnos_paciente_id_fkey",
                table: "turnos",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "turno_paciente_obrasocial_fkey",
                table: "turnos");

            migrationBuilder.DropForeignKey(
                name: "turnos_paciente_id_fkey",
                table: "turnos");

            migrationBuilder.DropTable(
                name: "medico_obrasocial");

            migrationBuilder.DropTable(
                name: "paciente_obrasocial");

            migrationBuilder.DropTable(
                name: "obrassociales");

            migrationBuilder.DropIndex(
                name: "IX_turnos_pacienteobrasocial_id",
                table: "turnos");

            migrationBuilder.DropColumn(
                name: "es_particular",
                table: "turnos");

            migrationBuilder.DropColumn(
                name: "pacienteobrasocial_id",
                table: "turnos");

            migrationBuilder.AddForeignKey(
                name: "turnos_paciente_id_fkey",
                table: "turnos",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

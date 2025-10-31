using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaTrazabilidadTurnos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoriasturnos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    turno_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_nombre = table.Column<string>(type: "text", nullable: false),
                    momento_accion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "text", nullable: false),
                    fecha_anterior = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_nueva = table.Column<DateOnly>(type: "date", nullable: true),
                    hora_anterior = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    hora_nueva = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    estado_anterior = table.Column<string>(type: "text", nullable: true),
                    estado_nuevo = table.Column<string>(type: "text", nullable: true),
                    PacienteId = table.Column<int>(type: "integer", nullable: false),
                    PacienteNombre = table.Column<string>(type: "text", nullable: false),
                    MedicoId = table.Column<int>(type: "integer", nullable: false),
                    MedicoNombre = table.Column<string>(type: "text", nullable: false),
                    EspecialidadId = table.Column<int>(type: "integer", nullable: false),
                    EspecialidadNombre = table.Column<string>(type: "text", nullable: false),
                    slot_id_anterior = table.Column<int>(type: "integer", nullable: true),
                    slot_id_nuevo = table.Column<int>(type: "integer", nullable: true),
                    motivo_cancelacion = table.Column<string>(type: "text", nullable: true),
                    PacienteObraSocialId = table.Column<int>(type: "integer", nullable: true),
                    ObraSocialNombre = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoriasturnos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trazabilidadturnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    turno_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_nombre = table.Column<string>(type: "text", nullable: false),
                    usuario_rol = table.Column<string>(type: "text", nullable: false),
                    momento_accion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trazabilidadturnos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoriasturnos");

            migrationBuilder.DropTable(
                name: "trazabilidadturnos");
        }
    }
}

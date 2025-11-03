using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class addedDiasdisponiblesmedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diasdisponibilidadmedico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    medico_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<int>(type: "integer", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    duracion_turno_minutos = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    vigencia_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigencia_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diasdisponibilidadmedico", x => x.id);
                    table.ForeignKey(
                        name: "diadisponibilidadmedico_medico_fkey",
                        column: x => x.medico_id,
                        principalTable: "medicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diasdisponibilidadmedico_medico_id",
                table: "diasdisponibilidadmedico",
                column: "medico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diasdisponibilidadmedico");
        }
    }
}

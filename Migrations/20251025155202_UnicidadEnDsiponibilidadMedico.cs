using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class UnicidadEnDsiponibilidadMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_disponibilidadesmedico_dia_semana_hora_inicio_hora_fin_medi~",
                table: "disponibilidadesmedico",
                columns: new[] { "dia_semana", "hora_inicio", "hora_fin", "medico_id", "activa" },
                unique: true,
                filter: "activa = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_disponibilidadesmedico_dia_semana_hora_inicio_hora_fin_medi~",
                table: "disponibilidadesmedico");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class ChangePropertiesNameAuditTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PacienteObraSocialId",
                table: "auditoriasturnos",
                newName: "paciente_obra_social_id");

            migrationBuilder.RenameColumn(
                name: "PacienteId",
                table: "auditoriasturnos",
                newName: "paciente_id");

            migrationBuilder.RenameColumn(
                name: "ObraSocialNombre",
                table: "auditoriasturnos",
                newName: "obra_social_nombre");

            migrationBuilder.RenameColumn(
                name: "MedicoId",
                table: "auditoriasturnos",
                newName: "medico_id");

            migrationBuilder.RenameColumn(
                name: "EspecialidadNombre",
                table: "auditoriasturnos",
                newName: "especialidad_nombre");

            migrationBuilder.RenameColumn(
                name: "EspecialidadId",
                table: "auditoriasturnos",
                newName: "especialidad_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "paciente_obra_social_id",
                table: "auditoriasturnos",
                newName: "PacienteObraSocialId");

            migrationBuilder.RenameColumn(
                name: "paciente_id",
                table: "auditoriasturnos",
                newName: "PacienteId");

            migrationBuilder.RenameColumn(
                name: "obra_social_nombre",
                table: "auditoriasturnos",
                newName: "ObraSocialNombre");

            migrationBuilder.RenameColumn(
                name: "medico_id",
                table: "auditoriasturnos",
                newName: "MedicoId");

            migrationBuilder.RenameColumn(
                name: "especialidad_nombre",
                table: "auditoriasturnos",
                newName: "EspecialidadNombre");

            migrationBuilder.RenameColumn(
                name: "especialidad_id",
                table: "auditoriasturnos",
                newName: "EspecialidadId");
        }
    }
}

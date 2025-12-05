using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class ModifyAuditTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PacienteNombre",
                table: "auditoriasturnos",
                newName: "paciente_nombre");

            migrationBuilder.RenameColumn(
                name: "MedicoNombre",
                table: "auditoriasturnos",
                newName: "medico_nombre");

            migrationBuilder.AddColumn<string>(
                name: "paciente_dni",
                table: "auditoriasturnos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "paciente_dni",
                table: "auditoriasturnos");

            migrationBuilder.RenameColumn(
                name: "paciente_nombre",
                table: "auditoriasturnos",
                newName: "PacienteNombre");

            migrationBuilder.RenameColumn(
                name: "medico_nombre",
                table: "auditoriasturnos",
                newName: "MedicoNombre");
        }
    }
}

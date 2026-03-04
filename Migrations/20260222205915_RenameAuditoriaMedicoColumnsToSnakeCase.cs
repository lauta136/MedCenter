using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuditoriaMedicoColumnsToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MedicoNuevoNombre",
                table: "auditoriasturnos",
                newName: "medico_nuevo_nombre");

            migrationBuilder.RenameColumn(
                name: "MedicoNuevoId",
                table: "auditoriasturnos",
                newName: "medico_nuevo_id");

            migrationBuilder.RenameColumn(
                name: "MedicoAnteriorNombre",
                table: "auditoriasturnos",
                newName: "medico_anterior_nombre");

            migrationBuilder.RenameColumn(
                name: "MedicoAnteriorId",
                table: "auditoriasturnos",
                newName: "medico_anterior_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "medico_nuevo_nombre",
                table: "auditoriasturnos",
                newName: "MedicoNuevoNombre");

            migrationBuilder.RenameColumn(
                name: "medico_nuevo_id",
                table: "auditoriasturnos",
                newName: "MedicoNuevoId");

            migrationBuilder.RenameColumn(
                name: "medico_anterior_nombre",
                table: "auditoriasturnos",
                newName: "MedicoAnteriorNombre");

            migrationBuilder.RenameColumn(
                name: "medico_anterior_id",
                table: "auditoriasturnos",
                newName: "MedicoAnteriorId");
        }
    }
}

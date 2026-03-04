using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedicoCambioEnAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicoAnteriorId",
                table: "auditoriasturnos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoAnteriorNombre",
                table: "auditoriasturnos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MedicoNuevoId",
                table: "auditoriasturnos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicoNuevoNombre",
                table: "auditoriasturnos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MedicoAnteriorId",
                table: "auditoriasturnos");

            migrationBuilder.DropColumn(
                name: "MedicoAnteriorNombre",
                table: "auditoriasturnos");

            migrationBuilder.DropColumn(
                name: "MedicoNuevoId",
                table: "auditoriasturnos");

            migrationBuilder.DropColumn(
                name: "MedicoNuevoNombre",
                table: "auditoriasturnos");
        }
    }
}

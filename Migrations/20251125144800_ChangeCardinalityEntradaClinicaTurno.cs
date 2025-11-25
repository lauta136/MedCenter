using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCardinalityEntradaClinicaTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas");

           migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_entradasclinicas_turno_id"";");

            migrationBuilder.DropColumn(
                name: "turno_id",
                table: "entradasclinicas");

            migrationBuilder.AddColumn<int>(
                name: "entradaClinica_id",
                table: "turnos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_turnos_entradaClinica_id",
                table: "turnos",
                column: "entradaClinica_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "turno_entradaclinica_fkey",
                table: "turnos",
                column: "entradaClinica_id",
                principalTable: "entradasclinicas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "turno_entradaclinica_fkey",
                table: "turnos");

            migrationBuilder.DropIndex(
                name: "IX_turnos_entradaClinica_id",
                table: "turnos");

            migrationBuilder.DropColumn(
                name: "entradaClinica_id",
                table: "turnos");

            migrationBuilder.AddColumn<int>(
                name: "turno_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_entradasclinicas_turno_id",
                table: "entradasclinicas",
                column: "turno_id");

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas",
                column: "turno_id",
                principalTable: "turnos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

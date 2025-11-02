using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class Relacion11TurnoSlotAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "turnos_slot_id_fkey",
                table: "turnos");

            // ✅ Eliminar índice SOLO si existe
            migrationBuilder.Sql(@"
            DROP INDEX IF EXISTS ""IX_turnos_slot_id"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_turnos_slot_id",
                table: "turnos",
                column: "slot_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "turno_slotagenda_fkey",
                table: "turnos",
                column: "slot_id",
                principalTable: "slotsagenda",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "turno_slotagenda_fkey",
                table: "turnos");

            migrationBuilder.DropIndex(
                name: "IX_turnos_slot_id",
                table: "turnos");

            migrationBuilder.CreateIndex(
                name: "IX_turnos_slot_id",
                table: "turnos",
                column: "slot_id");

            migrationBuilder.AddForeignKey(
                name: "turnos_slot_id_fkey",
                table: "turnos",
                column: "slot_id",
                principalTable: "slotsagenda",
                principalColumn: "id");
        }
    }
}

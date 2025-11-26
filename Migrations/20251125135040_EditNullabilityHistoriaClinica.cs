using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class EditNullabilityHistoriaClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "historiasclinicas_paciente_id_fkey",
                table: "historiasclinicas");

            migrationBuilder.AlterColumn<int>(
                name: "paciente_id",
                table: "historiasclinicas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "historiasclinicas_paciente_id_fkey",
                table: "historiasclinicas",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "historiasclinicas_paciente_id_fkey",
                table: "historiasclinicas");

            migrationBuilder.AlterColumn<int>(
                name: "paciente_id",
                table: "historiasclinicas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "historiasclinicas_paciente_id_fkey",
                table: "historiasclinicas",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "id");
        }
    }
}

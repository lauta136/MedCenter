using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class RelacionDisponibilidadSlotAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bloqueDisponibilidadId",
                table: "slotsagenda",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "vigencia_hasta",
                table: "disponibilidadesmedico",
                type: "date",
                nullable: true,
                defaultValue: new DateOnly(2025, 12, 26),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_slotsagenda_bloqueDisponibilidadId",
                table: "slotsagenda",
                column: "bloqueDisponibilidadId");

            migrationBuilder.AddForeignKey(
                name: "slotsagenda_disponibilidadmedico_fkey",
                table: "slotsagenda",
                column: "bloqueDisponibilidadId",
                principalTable: "disponibilidadesmedico",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "slotsagenda_disponibilidadmedico_fkey",
                table: "slotsagenda");

            migrationBuilder.DropIndex(
                name: "IX_slotsagenda_bloqueDisponibilidadId",
                table: "slotsagenda");

            migrationBuilder.DropColumn(
                name: "bloqueDisponibilidadId",
                table: "slotsagenda");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "vigencia_hasta",
                table: "disponibilidadesmedico",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true,
                oldDefaultValue: new DateOnly(2025, 12, 26));
        }
    }
}

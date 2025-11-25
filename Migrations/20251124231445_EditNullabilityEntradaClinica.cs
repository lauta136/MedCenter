using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class EditNullabilityEntradaClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_historia_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_medico_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.AlterColumn<int>(
                name: "turno_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tratamiento",
                table: "entradasclinicas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "medico_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "historia_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha",
                table: "entradasclinicas",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "diagnostico",
                table: "entradasclinicas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_historia_id_fkey",
                table: "entradasclinicas",
                column: "historia_id",
                principalTable: "historiasclinicas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_medico_id_fkey",
                table: "entradasclinicas",
                column: "medico_id",
                principalTable: "medicos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas",
                column: "turno_id",
                principalTable: "turnos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_historia_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_medico_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.DropForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas");

            migrationBuilder.AlterColumn<int>(
                name: "turno_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "tratamiento",
                table: "entradasclinicas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "medico_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "historia_id",
                table: "entradasclinicas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "fecha",
                table: "entradasclinicas",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "diagnostico",
                table: "entradasclinicas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_historia_id_fkey",
                table: "entradasclinicas",
                column: "historia_id",
                principalTable: "historiasclinicas",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_medico_id_fkey",
                table: "entradasclinicas",
                column: "medico_id",
                principalTable: "medicos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "entradasclinicas_turno_id_fkey",
                table: "entradasclinicas",
                column: "turno_id",
                principalTable: "turnos",
                principalColumn: "id");
        }
    }
}

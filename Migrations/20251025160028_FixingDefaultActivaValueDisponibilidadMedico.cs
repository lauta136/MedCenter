using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class FixingDefaultActivaValueDisponibilidadMedico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "activa",
                table: "disponibilidadesmedico",
                type: "boolean",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "activa",
                table: "disponibilidadesmedico",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: true);
        }
    }
}

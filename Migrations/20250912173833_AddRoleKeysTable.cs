using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedCenter.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleKeysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                migrationBuilder.CreateTable(
                name: "role_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentityAlwaysColumn", true),
                    role = table.Column<string>(type: "text", nullable: false),
                    hashed_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_keys", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.DropTable(
                name: "role_keys");
        }
    }   
}

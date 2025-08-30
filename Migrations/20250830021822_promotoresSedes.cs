using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class promotoresSedes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_promotor",
                table: "alumnos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "id_sede",
                table: "alumnos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "promotores",
                columns: table => new
                {
                    id_promotor = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotores", x => x.id_promotor);
                });

            migrationBuilder.CreateTable(
                name: "sedes",
                columns: table => new
                {
                    id_sede = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sedes", x => x.id_sede);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_id_promotor",
                table: "alumnos",
                column: "id_promotor");

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_id_sede",
                table: "alumnos",
                column: "id_sede");

            migrationBuilder.AddForeignKey(
                name: "FK_alumnos_promotores_id_promotor",
                table: "alumnos",
                column: "id_promotor",
                principalTable: "promotores",
                principalColumn: "id_promotor",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_alumnos_sedes_id_sede",
                table: "alumnos",
                column: "id_sede",
                principalTable: "sedes",
                principalColumn: "id_sede",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_alumnos_promotores_id_promotor",
                table: "alumnos");

            migrationBuilder.DropForeignKey(
                name: "FK_alumnos_sedes_id_sede",
                table: "alumnos");

            migrationBuilder.DropTable(
                name: "promotores");

            migrationBuilder.DropTable(
                name: "sedes");

            migrationBuilder.DropIndex(
                name: "IX_alumnos_id_promotor",
                table: "alumnos");

            migrationBuilder.DropIndex(
                name: "IX_alumnos_id_sede",
                table: "alumnos");

            migrationBuilder.DropColumn(
                name: "id_promotor",
                table: "alumnos");

            migrationBuilder.DropColumn(
                name: "id_sede",
                table: "alumnos");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class clases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clases",
                columns: table => new
                {
                    id_clase = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    taller_id = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clases", x => x.id_clase);
                    table.ForeignKey(
                        name: "FK_clases_talleres_taller_id",
                        column: x => x.taller_id,
                        principalTable: "talleres",
                        principalColumn: "id_taller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cargos_clase_id",
                table: "cargos",
                column: "clase_id");

            migrationBuilder.CreateIndex(
                name: "IX_clases_taller_id",
                table: "clases",
                column: "taller_id");

            migrationBuilder.AddForeignKey(
                name: "FK_cargos_clases_clase_id",
                table: "cargos",
                column: "clase_id",
                principalTable: "clases",
                principalColumn: "id_clase",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cargos_clases_clase_id",
                table: "cargos");

            migrationBuilder.DropTable(
                name: "clases");

            migrationBuilder.DropIndex(
                name: "IX_cargos_clase_id",
                table: "cargos");
        }
    }
}

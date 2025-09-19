using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSedeIdATalleres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sede_id",
                table: "talleres",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            // Actualizar registros existentes para que tengan SedeId = 1
            migrationBuilder.Sql("UPDATE talleres SET sede_id = 1 WHERE sede_id = 0");

            migrationBuilder.CreateIndex(
                name: "IX_talleres_sede_id",
                table: "talleres",
                column: "sede_id");

            migrationBuilder.AddForeignKey(
                name: "FK_talleres_sedes_sede_id",
                table: "talleres",
                column: "sede_id",
                principalTable: "sedes",
                principalColumn: "id_sede",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_talleres_sedes_sede_id",
                table: "talleres");

            migrationBuilder.DropIndex(
                name: "IX_talleres_sede_id",
                table: "talleres");

            migrationBuilder.DropColumn(
                name: "sede_id",
                table: "talleres");
        }
    }
}

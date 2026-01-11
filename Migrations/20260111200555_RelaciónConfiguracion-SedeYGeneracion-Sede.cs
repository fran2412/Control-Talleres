using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class RelaciónConfiguracionSedeYGeneracionSede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_sede",
                table: "generaciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "id_sede",
                table: "configuraciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_generaciones_id_sede",
                table: "generaciones",
                column: "id_sede");

            migrationBuilder.CreateIndex(
                name: "IX_configuraciones_id_sede",
                table: "configuraciones",
                column: "id_sede");

            migrationBuilder.AddForeignKey(
                name: "FK_configuraciones_sedes_id_sede",
                table: "configuraciones",
                column: "id_sede",
                principalTable: "sedes",
                principalColumn: "id_sede",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_generaciones_sedes_id_sede",
                table: "generaciones",
                column: "id_sede",
                principalTable: "sedes",
                principalColumn: "id_sede",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_configuraciones_sedes_id_sede",
                table: "configuraciones");

            migrationBuilder.DropForeignKey(
                name: "FK_generaciones_sedes_id_sede",
                table: "generaciones");

            migrationBuilder.DropIndex(
                name: "IX_generaciones_id_sede",
                table: "generaciones");

            migrationBuilder.DropIndex(
                name: "IX_configuraciones_id_sede",
                table: "configuraciones");

            migrationBuilder.DropColumn(
                name: "id_sede",
                table: "generaciones");

            migrationBuilder.DropColumn(
                name: "id_sede",
                table: "configuraciones");
        }
    }
}

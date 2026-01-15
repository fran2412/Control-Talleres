using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionSede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuraciones_sede",
                columns: table => new
                {
                    id_configuracion_sede = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id_sede = table.Column<int>(type: "INTEGER", nullable: false),
                    clave = table.Column<string>(type: "TEXT", nullable: false),
                    valor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuraciones_sede", x => x.id_configuracion_sede);
                    table.ForeignKey(
                        name: "FK_configuraciones_sede_sedes_id_sede",
                        column: x => x.id_sede,
                        principalTable: "sedes",
                        principalColumn: "id_sede",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuraciones_sede_id_sede_clave",
                table: "configuraciones_sede",
                columns: new[] { "id_sede", "clave" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuraciones_sede");
        }
    }
}

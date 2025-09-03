using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class inscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id",
                table: "inscripciones");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id_eliminado",
                table: "inscripciones",
                columns: new[] { "alumno_id", "taller_id", "generacion_id", "eliminado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id_eliminado",
                table: "inscripciones");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id",
                table: "inscripciones",
                columns: new[] { "alumno_id", "taller_id", "generacion_id" },
                unique: true);
        }
    }
}

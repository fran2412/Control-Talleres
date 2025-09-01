using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AlumnoTallerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Talleres",
                columns: table => new
                {
                    TallerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Eliminado = table.Column<bool>(type: "INTEGER", nullable: false),
                    EliminadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Talleres", x => x.TallerId);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    InscripcionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlumnoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TallerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.InscripcionId);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Talleres_TallerId",
                        column: x => x.TallerId,
                        principalTable: "Talleres",
                        principalColumn: "TallerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Inscripciones_alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "alumnos",
                        principalColumn: "id_alumno",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_AlumnoId",
                table: "Inscripciones",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_TallerId",
                table: "Inscripciones",
                column: "TallerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropTable(
                name: "Talleres");
        }
    }
}

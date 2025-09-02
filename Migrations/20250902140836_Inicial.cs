using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuraciones",
                columns: table => new
                {
                    clave = table.Column<string>(type: "TEXT", nullable: false),
                    valor = table.Column<string>(type: "TEXT", nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuraciones", x => x.clave);
                });

            migrationBuilder.CreateTable(
                name: "generaciones",
                columns: table => new
                {
                    id_generacion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generaciones", x => x.id_generacion);
                });

            migrationBuilder.CreateTable(
                name: "promotores",
                columns: table => new
                {
                    id_promotor = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sedes", x => x.id_sede);
                });

            migrationBuilder.CreateTable(
                name: "talleres",
                columns: table => new
                {
                    id_taller = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    horario = table.Column<string>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_talleres", x => x.id_taller);
                });

            migrationBuilder.CreateTable(
                name: "alumnos",
                columns: table => new
                {
                    id_alumno = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    telefono = table.Column<string>(type: "TEXT", nullable: true),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    id_sede = table.Column<int>(type: "INTEGER", nullable: true),
                    id_promotor = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alumnos", x => x.id_alumno);
                    table.ForeignKey(
                        name: "FK_alumnos_promotores_id_promotor",
                        column: x => x.id_promotor,
                        principalTable: "promotores",
                        principalColumn: "id_promotor",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alumnos_sedes_id_sede",
                        column: x => x.id_sede,
                        principalTable: "sedes",
                        principalColumn: "id_sede",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inscripciones",
                columns: table => new
                {
                    id_inscripcion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    costo = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    saldo_actual = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    alumno_id = table.Column<int>(type: "INTEGER", nullable: false),
                    taller_id = table.Column<int>(type: "INTEGER", nullable: false),
                    generacion_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscripciones", x => x.id_inscripcion);
                    table.ForeignKey(
                        name: "FK_inscripciones_alumnos_alumno_id",
                        column: x => x.alumno_id,
                        principalTable: "alumnos",
                        principalColumn: "id_alumno",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inscripciones_generaciones_generacion_id",
                        column: x => x.generacion_id,
                        principalTable: "generaciones",
                        principalColumn: "id_generacion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inscripciones_talleres_taller_id",
                        column: x => x.taller_id,
                        principalTable: "talleres",
                        principalColumn: "id_taller",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_id_promotor",
                table: "alumnos",
                column: "id_promotor");

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_id_sede",
                table: "alumnos",
                column: "id_sede");

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_nombre",
                table: "alumnos",
                column: "nombre");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id",
                table: "inscripciones",
                columns: new[] { "alumno_id", "taller_id", "generacion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_generacion_id",
                table: "inscripciones",
                column: "generacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_taller_id",
                table: "inscripciones",
                column: "taller_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuraciones");

            migrationBuilder.DropTable(
                name: "inscripciones");

            migrationBuilder.DropTable(
                name: "alumnos");

            migrationBuilder.DropTable(
                name: "generaciones");

            migrationBuilder.DropTable(
                name: "talleres");

            migrationBuilder.DropTable(
                name: "promotores");

            migrationBuilder.DropTable(
                name: "sedes");
        }
    }
}

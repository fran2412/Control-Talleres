using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDiaSemanaTaller : Migration
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
                    dia_semana = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
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
                name: "clases",
                columns: table => new
                {
                    id_clase = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    taller_id = table.Column<int>(type: "INTEGER", nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
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
                    cancelada_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    motivo_cancelacion = table.Column<string>(type: "TEXT", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    id_pago = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    monto_total = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    metodo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    referencia = table.Column<string>(type: "TEXT", nullable: true),
                    notas = table.Column<string>(type: "TEXT", nullable: true),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    alumno_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.id_pago);
                    table.ForeignKey(
                        name: "FK_pagos_alumnos_alumno_id",
                        column: x => x.alumno_id,
                        principalTable: "alumnos",
                        principalColumn: "id_alumno",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cargos",
                columns: table => new
                {
                    id_cargo = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    monto = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    saldo_actual = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    estado = table.Column<string>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    alumno_id = table.Column<int>(type: "INTEGER", nullable: false),
                    inscripcion_id = table.Column<int>(type: "INTEGER", nullable: true),
                    clase_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargos", x => x.id_cargo);
                    table.ForeignKey(
                        name: "FK_cargos_alumnos_alumno_id",
                        column: x => x.alumno_id,
                        principalTable: "alumnos",
                        principalColumn: "id_alumno",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cargos_clases_clase_id",
                        column: x => x.clase_id,
                        principalTable: "clases",
                        principalColumn: "id_clase",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cargos_inscripciones_inscripcion_id",
                        column: x => x.inscripcion_id,
                        principalTable: "inscripciones",
                        principalColumn: "id_inscripcion",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pago_aplicaciones",
                columns: table => new
                {
                    id_pago_aplicacion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    monto_aplicado = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    estado = table.Column<string>(type: "TEXT", nullable: false),
                    pago_id = table.Column<int>(type: "INTEGER", nullable: false),
                    cargo_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pago_aplicaciones", x => x.id_pago_aplicacion);
                    table.ForeignKey(
                        name: "FK_pago_aplicaciones_cargos_cargo_id",
                        column: x => x.cargo_id,
                        principalTable: "cargos",
                        principalColumn: "id_cargo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pago_aplicaciones_pagos_pago_id",
                        column: x => x.pago_id,
                        principalTable: "pagos",
                        principalColumn: "id_pago",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_cargos_alumno_id",
                table: "cargos",
                column: "alumno_id");

            migrationBuilder.CreateIndex(
                name: "IX_cargos_clase_id",
                table: "cargos",
                column: "clase_id");

            migrationBuilder.CreateIndex(
                name: "IX_cargos_inscripcion_id",
                table: "cargos",
                column: "inscripcion_id");

            migrationBuilder.CreateIndex(
                name: "IX_clases_taller_id",
                table: "clases",
                column: "taller_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_alumno_id_taller_id_generacion_id_eliminado_cancelada_en",
                table: "inscripciones",
                columns: new[] { "alumno_id", "taller_id", "generacion_id", "eliminado", "cancelada_en" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_generacion_id",
                table: "inscripciones",
                column: "generacion_id");

            migrationBuilder.CreateIndex(
                name: "IX_inscripciones_taller_id",
                table: "inscripciones",
                column: "taller_id");

            migrationBuilder.CreateIndex(
                name: "IX_pago_aplicaciones_cargo_id",
                table: "pago_aplicaciones",
                column: "cargo_id");

            migrationBuilder.CreateIndex(
                name: "IX_pago_aplicaciones_pago_id",
                table: "pago_aplicaciones",
                column: "pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_alumno_id",
                table: "pagos",
                column: "alumno_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuraciones");

            migrationBuilder.DropTable(
                name: "pago_aplicaciones");

            migrationBuilder.DropTable(
                name: "cargos");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropTable(
                name: "clases");

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

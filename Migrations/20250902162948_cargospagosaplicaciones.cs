using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class cargospagosaplicaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        name: "FK_cargos_inscripciones_inscripcion_id",
                        column: x => x.inscripcion_id,
                        principalTable: "inscripciones",
                        principalColumn: "id_inscripcion",
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
                name: "pago_aplicaciones",
                columns: table => new
                {
                    id_pago_aplicacion = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    monto_aplicado = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
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
                name: "IX_cargos_alumno_id",
                table: "cargos",
                column: "alumno_id");

            migrationBuilder.CreateIndex(
                name: "IX_cargos_inscripcion_id",
                table: "cargos",
                column: "inscripcion_id");

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
                name: "pago_aplicaciones");

            migrationBuilder.DropTable(
                name: "cargos");

            migrationBuilder.DropTable(
                name: "pagos");
        }
    }
}

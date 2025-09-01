using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class Inscripciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Horario",
                table: "Talleres",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Costo",
                table: "Inscripciones",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "GeneracionId",
                table: "Inscripciones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Generacion",
                columns: table => new
                {
                    GeneracionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EsActual = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ActualizadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Eliminado = table.Column<bool>(type: "INTEGER", nullable: false),
                    EliminadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generacion", x => x.GeneracionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_GeneracionId",
                table: "Inscripciones",
                column: "GeneracionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscripciones_Generacion_GeneracionId",
                table: "Inscripciones",
                column: "GeneracionId",
                principalTable: "Generacion",
                principalColumn: "GeneracionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inscripciones_Generacion_GeneracionId",
                table: "Inscripciones");

            migrationBuilder.DropTable(
                name: "Generacion");

            migrationBuilder.DropIndex(
                name: "IX_Inscripciones_GeneracionId",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "Horario",
                table: "Talleres");

            migrationBuilder.DropColumn(
                name: "Costo",
                table: "Inscripciones");

            migrationBuilder.DropColumn(
                name: "GeneracionId",
                table: "Inscripciones");
        }
    }
}

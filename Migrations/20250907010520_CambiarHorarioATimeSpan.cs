using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class CambiarHorarioATimeSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "horario",
                table: "talleres",
                newName: "horario_inicio");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "horario_fin",
                table: "talleres",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "horario_fin",
                table: "talleres");

            migrationBuilder.RenameColumn(
                name: "horario_inicio",
                table: "talleres",
                newName: "horario");
        }
    }
}

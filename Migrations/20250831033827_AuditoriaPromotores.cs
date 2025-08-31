using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaPromotores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actualizado_en",
                table: "promotores",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "creado_en",
                table: "promotores",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<bool>(
                name: "eliminado",
                table: "promotores",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "eliminado_en",
                table: "promotores",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actualizado_en",
                table: "promotores");

            migrationBuilder.DropColumn(
                name: "creado_en",
                table: "promotores");

            migrationBuilder.DropColumn(
                name: "eliminado",
                table: "promotores");

            migrationBuilder.DropColumn(
                name: "eliminado_en",
                table: "promotores");
        }
    }
}

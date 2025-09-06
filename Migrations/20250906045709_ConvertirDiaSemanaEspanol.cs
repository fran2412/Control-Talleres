using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlTalleresMVP.Migrations
{
    /// <inheritdoc />
    public partial class ConvertirDiaSemanaEspanol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Actualizar los datos existentes de inglés a español
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Lunes' WHERE dia_semana = 'Monday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Martes' WHERE dia_semana = 'Tuesday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Miércoles' WHERE dia_semana = 'Wednesday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Jueves' WHERE dia_semana = 'Thursday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Viernes' WHERE dia_semana = 'Friday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Sábado' WHERE dia_semana = 'Saturday'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Domingo' WHERE dia_semana = 'Sunday'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir de español a inglés
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Monday' WHERE dia_semana = 'Lunes'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Tuesday' WHERE dia_semana = 'Martes'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Wednesday' WHERE dia_semana = 'Miércoles'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Thursday' WHERE dia_semana = 'Jueves'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Friday' WHERE dia_semana = 'Viernes'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Saturday' WHERE dia_semana = 'Sábado'");
            migrationBuilder.Sql("UPDATE talleres SET dia_semana = 'Sunday' WHERE dia_semana = 'Domingo'");
        }
    }
}

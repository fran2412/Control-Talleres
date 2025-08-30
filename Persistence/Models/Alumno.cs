using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Alumno
    {
        public int IdAlumno { get; set; }

        public string Nombre { get; set; } = null!;
        public string? Telefono { get; set; }

        // Clave foránea a Sede
        public int IdSede { get; set; }
        public Sede Sede { get; set; } = null!;

        // Clave foránea a Promotor
        public int IdPromotor { get; set; }
        public Promotor Promotor { get; set; } = null!;

        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }

        public bool Eliminado { get; set; } = false;
        public DateTimeOffset? EliminadoEn { get; set; }

    }
}

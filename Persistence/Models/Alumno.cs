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

        public DateTimeOffset CreadoEn { get; set; }

        public DateTimeOffset ActualizadoEn { get; set; }

        public bool Eliminado { get; set; } = false;

        public DateTimeOffset? EliminadoEn { get; set; }

    }
}

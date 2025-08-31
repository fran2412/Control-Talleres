using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.Models
{
    public class Sede
    {
        public int IdSede { get; set; }
        public string Nombre { get; set; } = null!;

        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }

        public bool Eliminado { get; set; } = false;
        public DateTimeOffset? EliminadoEn { get; set; }

        // Relación inversa: una sede puede tener muchos alumnos
        public ICollection<Alumno> Alumnos { get; set; } = new List<Alumno>();
    }
}
